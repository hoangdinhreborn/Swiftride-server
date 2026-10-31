# Kiến Trúc Hệ Thống SwiftRide (Microservices & Clean Architecture)

Tài liệu này mô tả chi tiết kiến trúc phần mềm của dự án **SwiftRide Backend**, bám sát theo tài liệu đặc tả môn học **Kiến trúc Phần mềm - Đại học Sài Gòn (VER 1.0)**.

---

## 1. Cây Thư Mục Chi Tiết Toàn Bộ Hệ Thống (Repository Layout)

```text
swiftride-server/
│   .gitignore                                       # Bỏ qua bin, obj, .vs, .env, secrets
│   .gitattributes                                   # Chuẩn hóa line-ending LF cho scripts
│   .dockerignore                                    # Tối ưu context build Docker (bỏ bin, obj, git)
│   .env.example                                     # Mẫu biến môi trường cho Docker & Production
│   docker-compose.yml                               # Hạ tầng gồm 2 PostgreSQL độc lập, MongoDB, RabbitMQ, Aspire
│   SwiftRide.slnx                                   # Solution quản trị toàn bộ projects (.NET 10)
│   README.md                                        # Hướng dẫn tổng quan và vận hành hệ thống
│
├── .github/
│   └── workflows/
│       └── ci.yml                                   # GitHub Actions CI: Build, Test (88 tests) & Docker E2E
│
├── docker/
│   └── Dockerfile                                   # Multi-stage build tối ưu cho toàn bộ các service
│
├── docs/
│   ├── run.txt                                      # Hướng dẫn lệnh chạy nhanh
│   ├── architecture.md                              # Tài liệu kiến trúc toàn diện (file này)
│   └── RideLite-KienTrucPhanMem.pdf                 # Đặc tả bài tập lớn
│
├── scripts/
│   └── smoke.sh                                     # Kịch bản kiểm thử E2E tự động từ đặt xe tới thanh toán
│
├── src/
│   │
│   ├── ApiGateway/                                  # [EDGE SERVICE: CỔNG API GATEWAY (YARP)]
│   │   │                                            # Port: 5000 (Docker) / 5233 (Local)
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.json                         # Định tuyến ReverseProxy, Rate Limiting & Clusters
│   │   ├── Program.cs                               # Composition Root: YARP, Edge Auth, RateLimiter
│   │   └── ApiGateway.csproj
│   │
│   ├── BuildingBlocks/                              # [THƯ VIỆN HẠ TẦNG DÙNG CHUNG]
│   │   ├── BuildingBlocks.Security/                 # Đóng gói xác thực & phân quyền JWT tập trung
│   │   │   ├── ICurrentUser.cs                      # Abstraction lấy UserId, Role từ Token Claims
│   │   │   ├── CurrentUser.cs                       # Hiện thực đọc Claims từ HttpContext
│   │   │   ├── Roles.cs                             # Hằng số vai trò: Rider, Driver, Admin
│   │   │   ├── Policies.cs                          # Hằng số chính sách: RiderOnly, DriverOnly, AnyRole
│   │   │   ├── SecurityExtensions.cs                # Extension methods: AddSwiftRideAuth, MapDevAuthEndpoints
│   │   │   └── BuildingBlocks.Security.csproj
│   │   │
│   │   └── BuildingBlocks.Observability/            # Đóng gói OpenTelemetry & Distributed Tracing
│   │       ├── ObservabilityExtensions.cs           # AddSwiftRideTelemetry (Tracing, Metrics, Logs, OTLP)
│   │       └── BuildingBlocks.Observability.csproj
│   │
│   ├── TripService/                                 # [MICROSERVICE 1: QUẢN LÝ CHUYẾN ĐI]
│   │   │                                            # Cơ sở dữ liệu: PostgreSQL riêng (tripdb)
│   │   ├── TripService.Api/                         # [Presentation Layer]: Minimal API
│   │   │   ├── Auth/                                # HttpContextAccessTokenProvider
│   │   │   ├── Endpoints/TripEndpoints.cs           # POST /trips, driver-accept, pickup, dropoff...
│   │   │   ├── ExceptionHandling/                   # GlobalExceptionHandler chuẩn ProblemDetails
│   │   │   ├── Properties/launchSettings.json       # Port: 5130 (Local)
│   │   │   ├── appsettings.json
│   │   │   ├── Program.cs                           # Composition Root: DI, Telemetry, Outbox
│   │   │   └── TripService.Api.csproj
│   │   │
│   │   ├── TripService.Application/                 # [Application Layer]: Use Cases & CQRS Handlers
│   │   │   ├── Common/
│   │   │   │   ├── Exceptions/                      # PricingUnavailableException, ConcurrencyException...
│   │   │   │   ├── Interfaces/                      # IHandler, IUnitOfWork, IOutbox, IPricingGateway
│   │   │   │   └── Telemetry/TripMetrics.cs         # Counter & Histogram đo lường nghiệp vụ chuyến đi
│   │   │   ├── Trips/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── AcceptTrip/                  # AcceptTripCommand + Handler
│   │   │   │   │   ├── ApplyPaymentResult/          # ApplyPaymentResultCommand (Consumer cập nhật Paid)
│   │   │   │   │   ├── DropOffTrip/                 # DropOffTripHandler (Ghi Outbox Event)
│   │   │   │   │   ├── PickUpTrip/                  # PickUpTripCommand + Handler
│   │   │   │   │   └── RequestTrip/                 # RequestTripHandler (Gọi IPricingGateway)
│   │   │   │   ├── Dtos/TripDto.cs, TripMappings.cs
│   │   │   │   ├── Events/RideCompletedEvent.cs     # Domain Event phát đi khi trả khách
│   │   │   │   ├── Interfaces/ITripRepository.cs
│   │   │   │   └── Queries/GetTrip/                 # GetTripQuery + Handler
│   │   │   ├── DependencyInjection.cs
│   │   │   └── TripService.Application.csproj
│   │   │
│   │   ├── TripService.Domain/                      # [Domain Layer]: Pure C# (Rich Domain Model)
│   │   │   ├── Common/DomainException.cs
│   │   │   ├── Trips/
│   │   │   │   ├── Trip.cs                          # Aggregate Root: Bảo vệ bất biến & trạng thái
│   │   │   │   └── TripStatus.cs                    # Enum: Requested -> Accepted -> PickedUp -> DroppedOff -> Paid
│   │   │   └── TripService.Domain.csproj
│   │   │
│   │   └── TripService.Infrastructure/              # [Infrastructure Layer]: Persistence & Messaging
│   │       ├── Gateways/MatchingPricingGateway.cs   # HTTP Client gọi MatchingService với BearerTokenHandler
│   │       ├── Messaging/                           # RabbitMqPublisher, OutboxPublisher, PaymentResultConsumer
│   │       ├── Persistence/
│   │       │   ├── Configurations/                  # TripConfiguration (xmin Concurrency Token)
│   │       │   ├── Migrations/                      # EF Core Code-First (Trips, OutboxMessages)
│   │       │   ├── Outbox/OutboxMessage.cs          # Thực thể lưu sự kiện Outbox trong DB
│   │       │   ├── Repositories/TripRepository.cs
│   │       │   ├── MigrationExtensions.cs           # Tự động migrate khi khởi động
│   │       │   └── TripDbContext.cs                 # DbContext & UnitOfWork & IOutbox
│   │       ├── DependencyInjection.cs
│   │       └── TripService.Infrastructure.csproj
│   │
│   ├── MatchingService/                             # [MICROSERVICE 2: GHÉP XE & TÍNH CƯỚC]
│   │   │                                            # Cơ sở dữ liệu: MongoDB (MatchDb - Geo-Spatial)
│   │   ├── MatchingService.Api/                     # [Presentation Layer]: Minimal API
│   │   │   ├── Endpoints/
│   │   │   │   ├── DriverEndpoints.cs               # PUT /drivers/me/location, POST /match
│   │   │   │   └── PricingEndpoints.cs              # POST /quote
│   │   │   ├── ExceptionHandling/
│   │   │   ├── Properties/launchSettings.json       # Port: 5197 (Local)
│   │   │   ├── appsettings.json
│   │   │   ├── Program.cs
│   │   │   └── MatchingService.Api.csproj
│   │   │
│   │   ├── MatchingService.Application/             # [Application Layer]: CQRS Handlers
│   │   │   ├── Common/Telemetry/MatchingMetrics.cs
│   │   │   ├── Drivers/                             # UpdateDriverLocation, MatchDrivers
│   │   │   ├── Pricing/GetQuote/                    # Tính giá dựa trên Strategy & Cung cầu tài xế
│   │   │   ├── DependencyInjection.cs
│   │   │   └── MatchingService.Application.csproj
│   │   │
│   │   ├── MatchingService.Domain/                  # [Domain Layer]: Strategy Pattern & Geo Models
│   │   │   ├── Drivers/DriverLocation.cs
│   │   │   ├── Geo/GeoPoint.cs                      # Value Object tính khoảng cách Haversine
│   │   │   ├── Pricing/
│   │   │   │   ├── Strategies/                      # Strategy Pattern (Standard, Surge, Promo)
│   │   │   │   ├── FareCalculator.cs                # Công thức: p = ((cd*d + ct*t)*s + cm)*η*(1+T)
│   │   │   │   ├── PricingStrategyFactory.cs        # Factory tự chọn hệ số Surge
│   │   │   │   └── RouteEstimator.cs
│   │   │   └── MatchingService.Domain.csproj
│   │   │
│   │   └── MatchingService.Infrastructure/          # [Infrastructure Layer]: MongoDB Driver
│   │       ├── Persistence/
│   │       │   ├── Documents/                       # DriverLocationDocument (GeoJsonPoint)
│   │       │   ├── Repositories/                    # DriverLocationRepository ($near 2dsphere index)
│   │       │   └── MatchingMongoContext.cs
│   │       ├── DependencyInjection.cs
│   │       └── MatchingService.Infrastructure.csproj
│   │
│   └── PaymentService/                              # [MICROSERVICE 3: THANH TOÁN & SỔ CÁI]
│       │                                            # Cơ sở dữ liệu: PostgreSQL riêng (paydb)
│       ├── PaymentService.Api/                      # [Presentation Layer]: Minimal API
│       │   ├── Auth/HttpContextAccessTokenProvider.cs
│       │   ├── Endpoints/PaymentEndpoints.cs        # POST /payments, GET /payments/{id}, POST /{id}/refund
│       │   ├── ExceptionHandling/
│       │   ├── Properties/launchSettings.json       # Port: 5273 (Local)
│       │   ├── appsettings.json
│       │   ├── Program.cs
│       │   └── PaymentService.Api.csproj
│       │
│       ├── PaymentService.Application/              # [Application Layer]: Sổ cái & Xử lý Giao dịch
│       │   ├── Common/Exceptions/, Interfaces/
│       │   ├── Payments/
│       │   │   ├── Commands/
│       │   │   │   ├── CreatePayment/               # Xử lý Idempotency, gọi cổng thanh toán, ghi sổ cái
│       │   │   │   └── RefundPayment/               # Hoàn tiền, đảo bút toán sổ cái
│       │   │   ├── Dtos/PaymentDto.cs, PaymentMappings.cs
│       │   │   └── Queries/GetPayment/
│       │   ├── DependencyInjection.cs
│       │   └── PaymentService.Application.csproj
│       │
│       ├── PaymentService.Domain/                   # [Domain Layer]: Double-Entry Ledger
│       │   ├── Payments/
│       │   │   ├── Payment.cs                       # Thực thể thanh toán (Trạng thái Pending, Succeeded, Failed)
│       │   │   ├── LedgerEntry.cs                   # Bút toán Nợ/Có (Debit/Credit)
│       │   │   └── PaymentEnums.cs                  # LedgerAccount, EntryDirection, PaymentStatus
│       │   └── PaymentService.Domain.csproj
│       │
│       └── PaymentService.Infrastructure/          # [Infrastructure Layer]: EF Core & Gateways
│           ├── Gateways/TripHttpGateway.cs          # Gọi TripService lấy số tiền cước thực tế
│           ├── Persistence/
│           │   ├── Configurations/PaymentConfiguration.cs
│           │   ├── Migrations/                      # EF Core Code-First (Payments, LedgerEntries)
│           │   ├── Repositories/PaymentRepository.cs
│           │   └── PaymentDbContext.cs
│           ├── Providers/FakePaymentProvider.cs     # Mock cổng thanh toán thẻ/ví điện tử
│           ├── DependencyInjection.cs
│           └── PaymentService.Infrastructure.csproj
│
└── tests/
    ├── TripService.Tests/                           # 39 tests: Domain, CQRS Handlers, Outbox, Concurrency
    ├── MatchingService.Tests/                       # 20 tests: Strategy Pricing, Haversine, Architecture
    └── PaymentService.Tests/                        # 29 tests: Double-Entry Ledger, Idempotency, Handlers
```

---

## 2. Kiến Trúc 3 Cơ Sở Dữ Liệu Độc Lập (Database-per-Service)

Hệ thống tuân thủ triệt để nguyên tắc **Database-per-Service** với 3 container dữ liệu hoàn toàn tách biệt:

| Microservice | Công nghệ CSDL | Container Name | Cổng Host | Lý do lựa chọn theo đặc tả môn học |
| :--- | :--- | :--- | :---: | :--- |
| **`TripService`** | **PostgreSQL 16** | `swiftride-postgres-trip` | `5432` | Đảm bảo tính toàn vẹn ACID, chuyển trạng thái vòng đời chuyến đi và lưu trữ bảng Transactional Outbox. |
| **`PaymentService`** | **PostgreSQL 16** | `swiftride-postgres-payment` | `5433` | Lưu trữ sổ cái kế toán Double-Entry (Bút toán Nợ/Có) độc lập hoàn toàn, đảm bảo kiểm toán tài chính nghiêm ngặt. |
| **`MatchingService`** | **MongoDB 7** | `swiftride-mongo` | `27017` | Tối ưu hóa ghi vị trí GPS tài xế liên tục, hỗ trợ chỉ mục địa không gian `2dsphere` với truy vấn `$near`. |

---

## 3. Các Mẫu Thiết Kế (Design Patterns) Trọng Tâm

### 3.1. Transactional Outbox Pattern & Event Saga (`TripService` $\leftrightarrow$ `PaymentService`)
- Khi tài xế bấm "Trả khách" (`POST /trips/{id}/dropoff`), `TripService` thực hiện trong **cùng một PostgreSQL transaction**:
  1. Đổi trạng thái chuyến đi sang `DroppedOff`.
  2. Ghi một bản ghi sự kiện `RideCompletedEvent` vào bảng `OutboxMessages` (kèm W3C `traceparent` để theo dõi trace).
- Background worker `OutboxPublisher` định kỳ quét bảng outbox và publish sự kiện lên **RabbitMQ** (Exchange: `swiftride.events`).
- `PaymentService` (hoặc consumer tự động) nhận sự kiện để trừ tiền. Sau khi thanh toán thành công, kết quả được bắn ngược về RabbitMQ để `PaymentResultConsumer` trong `TripService` cập nhật trạng thái chuyến đi sang `Paid`.

### 3.2. Sổ Cái Kế Toán Double-Entry & Idempotency (`PaymentService`)
- Mỗi giao dịch thành công tự động sinh ra 2 bút toán cân bằng:
  - **Ghi Nợ (Debit)**: Tài khoản `GatewayClearing` (Cổng thanh toán đang giữ tiền).
  - **Ghi Có (Credit)**: Tài khoản `TripRevenue` (Doanh thu chuyến đi).
- Khi có yêu cầu Hoàn tiền (`Refund`), hệ thống thực hiện đảo bút toán tương ứng.
- **Idempotency Key**: Bắt buộc client gửi header `Idempotency-Key`. Nếu cùng một key được gửi lại, hệ thống trả về ngay kết quả trước đó mà không gọi lại cổng thanh toán.

### 3.3. Strategy Pattern (`MatchingService`)
- Hợp đồng `IPricingStrategy` được triển khai bởi `StandardPricing`, `SurgePricing` (nhân hệ số cao điểm), và `PromoPricing` (Decorator giảm giá khuyến mãi).
- `PricingStrategyFactory` tự động chọn strategy dựa trên mật độ tài xế sẵn sàng trong bán kính 3km quanh điểm đón.

### 3.4. API Gateway & Edge Security (`ApiGateway`)
- Dùng Microsoft YARP (Yet Another Reverse Proxy).
- Xác thực tập trung JWT Bearer thông qua `BuildingBlocks.Security` ngay tại cửa ngõ. Request không có token hợp lệ bị chặn ngay lập tức (HTTP 401).
- Rate Limiting 60 request/phút cho từng Client (dựa trên JWT `sub` hoặc IP).

---

## 4. Hệ Thống Quan Sát (Observability & Distributed Tracing)

- Toàn bộ 4 dịch vụ (`ApiGateway`, `TripService`, `MatchingService`, `PaymentService`) đều tích hợp `BuildingBlocks.Observability`.
- Giao thức chuẩn OTLP gRPC xuất toàn bộ Traces, Metrics và Logs về **.NET Aspire Dashboard** (`http://localhost:18888`).
- Header `X-Trace-Id` được tự động trả về cho Client trong mọi phản hồi HTTP để thuận tiện cho việc tra cứu log khi có sự cố.
