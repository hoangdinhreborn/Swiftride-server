# SwiftRide Backend - Hệ Thống Đặt Xe Công Nghệ Microservices (.NET 10)

Hệ thống Backend cho nền tảng gọi xe công nghệ **SwiftRide**, xây dựng theo kiến trúc **Microservices**, **Clean Architecture**, **Domain-Driven Design (DDD)**, **State Pattern (GoF)**, **Factory Pattern (PaymentMethod)**, **Bounded Retry (PaymentSettler)** và kiến trúc hướng sự kiện (**Event-Driven Architecture**).

---

## 1. Sơ Đồ Kiến Trúc Hệ Thống (C4 Container Diagram)

```
                                  [ CLIENTS ]
                           (Rider App / Driver App)
                                      |
                                      v
                        +----------------------------+
                        |     API GATEWAY (YARP)     |
                        |      Port: 5000 / 5233     |
                        |  - Edge Auth (JWT Bearer)  |
                        |  - Rate Limit (60 req/min) |
                        +----------------------------+
                          /           |            \
                         /            |             \
      /trips/**         /    /quote   |              \  /payments/**
                       /     /match   |               \
                      v               v                v
      +--------------------+ +--------------------+ +--------------------+
      |    TripService     | |  MatchingService   | |   PaymentService   |
      |     Port: 5130     | |     Port: 5197     | |     Port: 5273     |
      | - Clean Arch (4L)  | | - Strategy Pricing | | - Double-Entry     |
      | - State Pattern    | | - Geo-Spatial Match| | - Payment Factory  |
      | - Dispatch Retry   | | - Exclude Declined | | - Bounded Retry    |
      | - Offer Expiry TTL | +--------------------+ | - Idempotency Key  |
      | - Outbox Pattern   |           |            | - Outbox Consumer  |
      +--------------------+           |            +--------------------+
           |          \                |                      |
           |           \               |                      |
           v            \              v                      v
    +-------------+      \      +-------------+        +-------------+
    | PostgreSQL  |       \     |   MongoDB   |        | PostgreSQL  |
    |  (tripdb)   |        \    |  (matchdb)  |        |  (paydb)    |
    |  Port: 5432 |         \   | Port: 27017 |        |  Port: 5433 |
    +-------------+          \  +-------------+        +-------------+
                              \
                               +-----------------------------+
                               |     RABBITMQ EVENT BUS      |
                               |    AMQP: 5672 | UI: 15672   |
                               | - Exchange: swiftride.events|
                               | - Outbox -> RideCompleted   |
                               | - PaymentResult -> Paid     |
                               +-----------------------------+
                                              |
                                              v
                               +-----------------------------+
                               |    .NET ASPIRE DASHBOARD    |
                               |     OTLP: 18889 | UI: 18888 |
                               |  Distributed Traces & Logs  |
                               +-----------------------------+
```

---

## 2. Bảng Danh Mục Dịch Vụ & Cổng Kết Nối

| Thành phần | Công nghệ | Cổng Host (Docker) | Cổng Local (`dotnet run`) | Mô tả |
| :--- | :--- | :---: | :---: | :--- |
| **ApiGateway** | YARP Reverse Proxy | **`5000`** | `5233` | Cổng vào duy nhất của hệ thống, chặn request không hợp lệ tại biên. |
| **TripService** | ASP.NET Core Minimal API | `8080` (Internal) | **`5130`** | Quản lý vòng đời chuyến đi bằng **State Pattern**, điều phối tài xế (retry $\le k$ lần) và quét hết hạn lời mời (TTL). |
| **MatchingService** | ASP.NET Core Minimal API | `8080` (Internal) | **`5197`** | Tìm tài xế gần nhất theo tọa độ GPS (loại trừ tài xế đã từ chối) và tính giá động (Surge/Promo Strategy). |
| **PaymentService** | ASP.NET Core Minimal API | `8080` (Internal) | **`5273`** | Thanh toán chuyến đi qua **PaymentMethod Factory**, ghi sổ cái kế toán Double-Entry, **Bounded Retry** với Idempotency key. |
| **PostgreSQL Trip** | PostgreSQL 16 | **`5432`** | `5432` | Cơ sở dữ liệu riêng của `TripService` (Database: `tripdb`). |
| **PostgreSQL Payment** | PostgreSQL 16 | **`5433`** | `5433` | Cơ sở dữ liệu riêng của `PaymentService` (Database: `paydb`). |
| **MongoDB** | MongoDB 7 | **`27017`** | `27017` | Cơ sở dữ liệu tài xế của `MatchingService` (Database: `matchdb`). |
| **RabbitMQ** | RabbitMQ 3 Management | **`5672`** / **`15672`** | `5672` / `15672` | Message Broker trao đổi sự kiện bất đồng bộ. Giao diện Web: `http://localhost:15672`. |
| **Aspire Dashboard** | .NET Aspire OpenTelemetry | **`18888`** | `18888` | Bảng điều khiển quan sát Distributed Traces, Metrics và Logs thời gian thực. |

---

## 3. Hướng Dẫn Chạy Bằng Docker Compose (Khuyến Nghị)

### Bước 1: Chuẩn bị file môi trường
```bash
cp .env.example .env
```

### Bước 2: Khởi động toàn bộ cụm Microservices
```bash
docker compose up -d --build
```
Hệ thống sẽ tự động:
1. Khởi động 2 container PostgreSQL (`postgres-trip` và `postgres-payment`), MongoDB, RabbitMQ và Aspire Dashboard.
2. Build các image multi-stage .NET 10 cho 3 service và Gateway.
3. Tự động áp dụng Migration vào 2 database PostgreSQL khi khởi động (`Database__MigrateOnStartup=true`).
4. Kết nối sẵn sàng qua API Gateway tại `http://localhost:5000`.

### Bước 3: Chạy Smoke Test kiểm thử tự động toàn bộ 3 kịch bản Flowchart
```bash
bash scripts/smoke.sh
```
Kịch bản kiểm thử sẽ tự động chạy 3 kịch bản theo đúng đặc tả môn học (Mục 4):
1. **Kịch bản 1 (Đủ luồng, có tài xế từ chối và retry)**:
   - Rider yêu cầu chuyến $\rightarrow$ Báo giá cước (`Requested`).
   - Rider đồng ý giá (`POST /trips/{id}/confirm`) $\rightarrow$ Hệ thống điều phối cho Tài xế $A$ (`DriverOffered`).
   - Tài xế $A$ từ chối (`POST /trips/{id}/driver-decline`) $\rightarrow$ Hệ thống tự động quét và đề xuất cho Tài xế $B$.
   - Tài xế $B$ nhận chuyến (`DriverAccepted`) $\rightarrow$ Đón khách (`PickedUp`) $\rightarrow$ Trả khách (`DroppedOff`).
   - `TripService` ghi Outbox Event $\rightarrow$ RabbitMQ chuyển sang `PaymentService` trừ tiền $\rightarrow$ Cập nhật trạng thái cuối cùng là **`Paid`**!
2. **Kịch bản 2 (Rider từ chối giá)**:
   - Rider hủy chuyến (`POST /trips/{id}/cancel`) $\rightarrow$ Chuyển sang trạng thái kết thúc **`Cancelled`**.
3. **Kịch bản 3 (Tài xế từ chối quá $k$ lần)**:
   - Các tài xế từ chối vượt quá số lần cho phép ($k = 3$, lần thứ 4 từ chối) $\rightarrow$ Hệ thống chuyển sang trạng thái kết thúc **`Failed`**.

### Bước 4: Mở Dashboard giám sát
- **Tra cứu Traces & Logs**: Truy cập `.NET Aspire Dashboard` tại `http://localhost:18888` để xem biểu đồ Gantt phân tán của request qua từng service.
- **Theo dõi Hàng đợi Message**: Truy cập `RabbitMQ Management` tại `http://localhost:15672` (User: `swiftride`, Password: `swiftride123`).

---

## 4. Hướng Dẫn Phát Triển Cục Bộ (Local Development)

Nếu bạn muốn chạy từng service bằng `dotnet run` từ terminal:

### 1. Khởi động hạ tầng dữ liệu
```bash
docker compose up -d postgres-trip postgres-payment mongo rabbitmq otel
```

### 2. Thiết lập Secret Key cho môi trường Dev
```bash
dotnet user-secrets set "Jwt:Key" "dev-only-secret-key-change-me-at-least-32-chars!" --project src/ApiGateway
dotnet user-secrets set "Jwt:Key" "dev-only-secret-key-change-me-at-least-32-chars!" --project src/TripService/TripService.Api
dotnet user-secrets set "Jwt:Key" "dev-only-secret-key-change-me-at-least-32-chars!" --project src/MatchingService/MatchingService.Api
dotnet user-secrets set "Jwt:Key" "dev-only-secret-key-change-me-at-least-32-chars!" --project src/PaymentService/PaymentService.Api
dotnet user-secrets set "ConnectionStrings:PayDb" "Host=localhost;Port=5433;Database=paydb;Username=swiftride;Password=swiftride123" --project src/PaymentService/PaymentService.Api
```

### 3. Cập nhật Database Migrations
```bash
dotnet ef database update --project src/TripService/TripService.Infrastructure --startup-project src/TripService/TripService.Api
dotnet ef database update --project src/PaymentService/PaymentService.Infrastructure --startup-project src/PaymentService/PaymentService.Api
```

### 4. Chạy Kiểm Thử (270 Tests)
```bash
dotnet test SwiftRide.slnx
```
Kết quả kiểm thử: **270/270 tests PASS 100%**:
- `TripService.Tests`: **161 tests** (State pattern, Dispatcher retry, Offer Expiry Scanner, Outbox, SAGA PaymentRejection).
- `PaymentService.Tests`: **72 tests** (Double-entry ledger, PaymentMethod Factory, Bounded Retry Settler, Idempotency, SAGA Compensation).
- `BuildingBlocks.Resilience.Tests`: **17 tests** (Polly retry, Exponential backoff, Circuit Breaker).
- `MatchingService.Tests`: **20 tests** (Strategy pricing, Haversine, Geo-query, Live Driver Location).

---

## 5. Cấu Trúc Nhánh Git (Branching Strategy)

- **`main`**: Nhánh chính tích hợp toàn bộ hệ thống hoàn chỉnh.
- **`feature/resilience-and-saga-compensation`**: Thư viện `BuildingBlocks.Resilience` (Polly retry, Circuit Breaker) và mẫu thiết kế SAGA Compensation (hoàn tiền và đảo bút toán tự động khi Trip từ chối thanh toán).
- **`feature/payment-factory-and-offer-expiry`**: Factory Pattern cho PaymentMethod, Bounded Retry cho PaymentSettler, và Offer Expiry Job cho TripService.
- **`feature/state-pattern-and-dispatch`**: Hiện thực State Pattern cho Trip và thuật toán điều phối ghép tài xế (retry $\le k$ lần).
- **`feature/shared-auth`**: Đóng gói thư viện dùng chung `BuildingBlocks.Security`.
- **`feature/payment-service`**: Microservice thanh toán và sổ cái kế toán độc lập.
- **`feature/trip-outbox-and-observability`**: Transactional Outbox, RabbitMQ và OpenTelemetry.
- **`feature/api-gateway`**: Cổng API Gateway với YARP và Rate Limiting.
- **`feature/docker-multidb-and-docs`**: Cấu hình 3 cơ sở dữ liệu độc lập và tài liệu toàn diện.
