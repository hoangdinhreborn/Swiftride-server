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
│       └── ci.yml                                   # GitHub Actions CI: Build, Test (270 tests) & Docker E2E
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
│   └── smoke.sh                                     # Kịch bản kiểm thử E2E tự động theo 3 nhánh flowchart
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
│   │   ├── BuildingBlocks.Observability/            # Đóng gói OpenTelemetry & Distributed Tracing
│   │   │   ├── ObservabilityExtensions.cs           # AddSwiftRideTelemetry (Tracing, Metrics, Logs, OTLP)
│   │   │   └── BuildingBlocks.Observability.csproj
│   │   │
│   │   └── BuildingBlocks.Resilience/               # Đóng gói Polly Resilience Policies (Retry, Jitter, Circuit Breaker)
│   │       ├── ResilienceExtensions.cs              # AddSwiftRideRetry, AddSwiftRideCircuitBreaker
│   │       └── BuildingBlocks.Resilience.csproj
│   │
│   ├── TripService/                                 # [MICROSERVICE 1: QUẢN LÝ CHUYẾN ĐI & ĐIỀU PHỐI]
│   │   │                                            # Cơ sở dữ liệu: PostgreSQL riêng (tripdb)
│   │   ├── TripService.Api/                         # [Presentation Layer]: Minimal API
│   │   │   ├── Auth/                                # HttpContextAccessTokenProvider, ServiceTokenIssuer
│   │   │   ├── Endpoints/TripEndpoints.cs           # POST /trips, confirm, cancel, driver-accept, driver-decline, pickup, dropoff, GET /offers
│   │   │   ├── ExceptionHandling/                   # GlobalExceptionHandler chuẩn ProblemDetails
│   │   │   ├── Properties/launchSettings.json       # Port: 5130 (Local)
│   │   │   ├── appsettings.json
│   │   │   ├── Program.cs                           # Composition Root: DI, Telemetry, Outbox, OfferScanner
│   │   │   └── TripService.Api.csproj
│   │   │
│   │   ├── TripService.Application/                 # [Application Layer]: Use Cases & CQRS Handlers
│   │   │   ├── Common/
│   │   │   │   ├── Exceptions/                      # PricingUnavailableException, ConcurrencyException...
│   │   │   │   ├── Interfaces/                      # IHandler, IUnitOfWork, IOutbox, IPricingGateway, IDriverMatchingGateway
│   │   │   │   └── Telemetry/TripMetrics.cs         # Counter & Histogram đo lường nghiệp vụ chuyến đi
│   │   │   ├── Trips/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── AcceptTrip/                  # AcceptTripCommand + Handler
│   │   │   │   │   ├── ApplyPaymentResult/          # ApplyPaymentResultCommand (Consumer cập nhật Paid)
│   │   │   │   │   ├── CancelTrip/                  # CancelTripCommand + Handler (Rider hủy/từ chối giá)
│   │   │   │   │   ├── ConfirmTrip/                 # ConfirmTripCommand + Handler (Rider đồng ý giá -> Dispatch)
│   │   │   │   │   ├── DeclineTrip/                 # DeclineTripCommand + Handler (Driver từ chối -> Retry ≤ k lần)
│   │   │   │   │   ├── ExpireOffer/                 # ExpireOfferCommand + Handler (Hết hạn offer -> Tự động chuyển TX tiếp theo)
│   │   │   │   │   ├── DropOffTrip/                 # DropOffTripHandler (Ghi Outbox Event)
│   │   │   │   │   ├── PickUpTrip/                  # PickUpTripCommand + Handler
│   │   │   │   │   └── RequestTrip/                 # RequestTripHandler (Báo giá cước)
│   │   │   │   ├── Dtos/TripDto.cs, TripMappings.cs
│   │   │   │   ├── Events/
│   │   │   │   │   ├── RideCompleted.cs             # Domain Event phát đi khi trả khách (Outbox Saga)
│   │   │   │   │   └── PaymentRejected.cs           # Event phát đi khi Trip từ chối thanh toán (Saga bồi thường)
│   │   │   │   ├── Interfaces/ITripRepository.cs
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetTrip/                     # GetTripQuery + Handler
│   │   │   │   │   └── GetMyOffers/                 # GetMyOffersQuery + Handler (Tài xế lấy cuốc đang đề xuất)
│   │   │   │   ├── DriverDispatcher.cs              # Bộ điều phối tài xế (quét matching, loại trừ tx đã từ chối)
│   │   │   │   └── OfferExpiryOptions.cs            # Cấu hình thời gian chờ tài xế nhận cuốc (TTL)
│   │   │   ├── DependencyInjection.cs
│   │   │   └── TripService.Application.csproj
│   │   │
│   │   ├── TripService.Domain/                      # [Domain Layer]: Pure C# (Rich Domain Model & State Pattern)
│   │   │   ├── Common/DomainException.cs
│   │   │   ├── Trips/
│   │   │   │   ├── States/                          # [STATE PATTERN (GoF)]:
│   │   │   │   │   ├── TripState.cs                 # Lớp cơ sở trừu tượng từ chối mọi hành động mặc định
│   │   │   │   │   ├── RequestedState.cs            # Chờ rider xác nhận giá
│   │   │   │   │   ├── SearchingState.cs            # Đang tìm tài xế (in-memory)
│   │   │   │   │   ├── DriverOfferedState.cs        # Đã đề xuất cho 1 tài xế, chờ accept/decline
│   │   │   │   │   ├── DriverAcceptedState.cs       # Tài xế đã nhận
│   │   │   │   │   ├── PickedUpState.cs             # Đã đón khách, đang di chuyển
│   │   │   │   │   ├── DroppedOffState.cs           # Đã trả khách, chờ thanh toán
│   │   │   │   │   ├── PaymentPendingState.cs       # Đang xử lý trừ tiền
│   │   │   │   │   ├── PaidState.cs                 # Kết thúc thành công: Đã thanh toán
│   │   │   │   │   ├── CancelledState.cs            # Kết thúc: Rider từ chối hoặc hủy
│   │   │   │   │   └── FailedState.cs               # Kết thúc không thành công: Từ chối > k lần
│   │   │   │   ├── Trip.cs                          # Aggregate Root: Chứa danh sách _declined & State
│   │   │   │   └── TripStatus.cs                    # Enum 10 trạng thái
│   │   │   └── TripService.Domain.csproj
│   │   │
│   │   └── TripService.Infrastructure/              # [Infrastructure Layer]: Persistence & Messaging
│   │       ├── Gateways/
│   │       │   ├── MatchingPricingGateway.cs        # HTTP Client gọi MatchingService lấy báo giá
│   │       │   └── MatchingDriverGateway.cs         # HTTP Client gọi MatchingService tìm tài xế gần
│   │       ├── Jobs/OfferExpiryScanner.cs           # Background Service quét các offer hết hạn TTL
│   │       ├── Messaging/                           # RabbitMqPublisher, OutboxPublisher, PaymentResultConsumer
│   │       ├── Persistence/
│   │       │   ├── Configurations/                  # TripConfiguration (xmin Concurrency Token)
│   │       │   ├── Migrations/                      # EF Core Code-First (Trips, OutboxMessages, DispatchFlow, OfferExpiry)
│   │       │   ├── Outbox/OutboxMessage.cs          # Thực thể lưu sự kiện Outbox trong DB
│   │       │   ├── Repositories/TripRepository.cs   # Truy vấn trip, active offers, expired offers
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
│   │   │   ├── Drivers/                             # UpdateDriverLocation, MatchDrivers (loại trừ tài xế từ chối)
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
│       ├── PaymentService.Application/              # [Application Layer]: Sổ cái, Bounded Retry & Idempotency
│       │   ├── Common/Exceptions/                   # PaymentProviderUnavailableException...
│       │   ├── Common/Interfaces/                   # IPaymentProvider, IPaymentRepository...
│       │   ├── Payments/
│       │   │   ├── Commands/
│       │   │   │   ├── CreatePayment/               # Xử lý Idempotency, gọi PaymentSettler, ghi sổ cái
│       │   │   │   ├── ProcessRideCompleted/        # Xử lý sự kiện từ RabbitMQ Consumer (AutoPay)
│       │   │   │   ├── CompensatePayment/           # Xử lý bồi thường/hoàn tiền khi Trip từ chối thanh toán
│       │   │   │   └── RefundPayment/               # Hoàn tiền thủ công, đảo bút toán sổ cái
│       │   │   ├── Dtos/PaymentDto.cs, PaymentMappings.cs
│       │   │   ├── Events/PaymentEvents.cs
│       │   │   ├── Queries/GetPayment/
│       │   │   ├── PaymentRefunder.cs               # Logic hoàn tiền qua cổng và đảo bút toán sổ cái kép
│       │   │   ├── PaymentSettler.cs                # Bounded Retry với Idempotency Key, chuyển PaymentPending nếu lỗi tạm thời
│       │   │   ├── PaymentRetryOptions.cs           # Cấu hình retry: maxAttempts, delayMs
│       │   │   └── AutoPayOptions.cs
│       │   ├── DependencyInjection.cs
│       │   └── PaymentService.Application.csproj
│       │
│       ├── PaymentService.Domain/                   # [Domain Layer]: Double-Entry Ledger & Factory Pattern
│       │   ├── Payments/
│       │   │   ├── Payment.cs                       # Thực thể thanh toán (Trạng thái Pending, Succeeded, Failed, PaymentPending, Refunded)
│       │   │   ├── PaymentMethod.cs                 # [FACTORY PATTERN]: Lớp cơ sở PaymentMethod
│       │   │   ├── PaymentMethodFactory.cs          # Tạo đối tượng CardPaymentMethod (tokenized) / WalletPaymentMethod
│       │   │   ├── LedgerEntry.cs                   # Bút toán Nợ/Có (Debit/Credit)
│       │   │   └── PaymentEnums.cs                  # LedgerAccount, EntryDirection, PaymentStatus
│       │   └── PaymentService.Domain.csproj
│       │
│       └── PaymentService.Infrastructure/          # [Infrastructure Layer]: EF Core & Gateways
│           ├── Gateways/TripHttpGateway.cs          # Gọi TripService lấy số tiền cước thực tế
│           ├── Messaging/                           # RabbitMqPublisher, OutboxPublisher, RideCompletedConsumer, PaymentRejectedConsumer
│           ├── Persistence/
│           │   ├── Configurations/PaymentConfiguration.cs
│           │   ├── Migrations/                      # EF Core Code-First (Payments, LedgerEntries, Outbox, PaymentMethod, PaymentAttempts, AddRefundReason)
│           │   ├── Repositories/PaymentRepository.cs
│           │   └── PaymentDbContext.cs
│           ├── Providers/FakePaymentProvider.cs     # Mock cổng thanh toán thẻ/ví điện tử
│           ├── DependencyInjection.cs
│           └── PaymentService.Infrastructure.csproj
│
└── tests/
    ├── TripService.Tests/                           # 161 tests: State Pattern, Dispatcher, Offer Expiry, Outbox, Saga PaymentRejected
    ├── PaymentService.Tests/                        # 72 tests: Double-Entry Ledger, Factory Pattern, Settler Retry, SAGA Compensation
    ├── BuildingBlocks.Resilience.Tests/             # 17 tests: Polly Retry, Jitter, Circuit Breaker
    └── MatchingService.Tests/                       # 20 tests: Strategy Pricing, Haversine, Driver Location Query
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

### 3.1. State Pattern (GoF Behavioral Pattern)
- Toàn bộ vòng đời chuyến đi được mô hình hóa theo **State Pattern**:
  - `TripState` là lớp cơ sở trừu tượng, mặc định từ chối (`throw DomainException`) mọi thao tác không hợp lệ.
  - Các trạng thái cụ thể (`RequestedState`, `SearchingState`, `DriverOfferedState`, `DriverAcceptedState`, `PickedUpState`, `DroppedOffState`, `PaymentPendingState`, `PaidState`, `CancelledState`, `FailedState`) chỉ ghi đè đúng những hành động mà trạng thái đó cho phép.
  - Entity `Trip` ủy quyền (delegate) việc kiểm tra và chuyển tiếp trạng thái cho đối tượng `State`.

### 3.2. Thuật Toán Điều Phối & Ghép Tài Xế (Driver Dispatcher Flowchart)
Tuân thủ chính xác lưu đồ Mục 4 của tài liệu đặc tả:
1. **Rider Đặt xe (`Requested`)**: Hệ thống tính toán báo giá ước tính.
2. **Rider Xác nhận giá (`POST /trips/{id}/confirm`)**:
   - Nếu Rider đồng ý: Chuyển sang `Searching`. `DriverDispatcher` gọi `MatchingService` (`POST /match`) để tìm danh sách tài xế gần nhất trong bán kính 5km chưa từ chối.
   - Tìm thấy tài xế: Gán `OfferedDriverId`, `OfferedAt = DateTime.UtcNow` và chuyển sang `DriverOffered`.
   - Không tìm thấy tài xế: Chuyển sang `Failed`.
3. **Rider Từ chối giá (`POST /trips/{id}/cancel`)**: Chuyển sang `Cancelled` (Kết thúc: Hủy chuyến).
4. **Tài xế nhận được đề xuất (`GET /trips/offers`)**:
   - **Tài xế đồng ý (`POST /trips/{id}/driver-accept`)**: Chuyển sang `DriverAccepted`. Bắt đầu đón khách.
   - **Tài xế từ chối (`POST /trips/{id}/driver-decline`)**:
     - Ghi nhận tài xế vào danh sách `_declined`.
     - Nếu số lần từ chối $\le k$ ($k = 3$): Tự động quay lại tìm tài xế kế tiếp gần nhất chưa từ chối.
     - Nếu số lần từ chối $> k$: Chuyển sang trạng thái `Failed` (Kết thúc: Không thành công).
5. **Hết hạn đề xuất (Offer Expiry Background Job)**:
   - Worker `OfferExpiryScanner` chạy ngầm định kỳ quét các cuốc xe có `OfferedAt` vượt quá TTL (`OFFER_TTL`, mặc định 10s - 30s) mà tài xế không phản hồi.
   - Tự động gọi `ExpireOfferHandler` để coi như tài xế đã từ chối và tự động chuyển cuốc xe cho tài xế tiếp theo.

### 3.3. Factory Pattern Cho Phương Thức Thanh Toán (`PaymentService`)
- Lớp cơ sở trừu tượng `PaymentMethod` được khởi tạo thông qua **`PaymentMethodFactory.Create(type, token)`**:
  - `CardPaymentMethod`: Thanh toán thẻ tín dụng/ghi nợ (tuân thủ bảo mật: chỉ lưu token do cổng thanh toán cấp, không lưu số thẻ/CVV).
  - `WalletPaymentMethod`: Thanh toán ví điện tử (MoMo, ZaloPay, ShopeePay).

### 3.4. Bounded Retry & Idempotency (`PaymentSettler`)
- Khi thanh toán gặp sự cố tạm thời (`PaymentProviderUnavailableException`):
  - Hệ thống thực hiện retry có giới hạn (mặc định 3 lần với backoff) với cùng một `Idempotency-Key`.
  - Nếu sau các lần thử vẫn lỗi tạm thời: Chuyến đi được chuyển sang trạng thái **`PaymentPending`** (chờ xử lý) thay vì trừ tiền lặp hoặc báo thành công sai, đúng theo lưu đồ Mục 4 & Mục 9.2 đặc tả.
  - Khi thành công: Chuyển sang `Succeeded` và ghi nhận 2 bút toán Nợ/Có vào sổ cái.

### 3.5. Transactional Outbox Pattern, Event Saga & Bồi Thường (Saga Compensation)
- Khi tài xế trả khách (`POST /trips/{id}/dropoff`), `TripService` thực hiện trong **cùng một PostgreSQL transaction**:
  1. Đổi trạng thái chuyến đi sang `DroppedOff`.
  2. Ghi bản ghi sự kiện `RideCompleted` vào bảng `OutboxMessages` (kèm W3C `traceparent`).
- Background worker `OutboxPublisher` định kỳ quét bảng outbox và publish sự kiện lên **RabbitMQ** (Exchange: `swiftride.events`).
- `PaymentService` (`RideCompletedConsumer`) nhận sự kiện để trừ tiền. Sau khi thanh toán thành công, kết quả được bắn ngược về RabbitMQ (`PaymentResultConsumer`) trong `TripService`.
- **Cơ chế bồi thường Saga (Saga Compensation Flow)**:
  - Nếu `TripService` từ chối áp dụng kết quả thanh toán (ví dụ: Chuyến đã bị hủy `Cancelled`, hoặc `DomainException` vi phạm State Pattern, hoặc không tìm thấy chuyến):
  - `TripService` tự động phát sự kiện **`PaymentRejected`** vào Outbox để gửi tới RabbitMQ (`trip.payment-rejected`).
  - `PaymentRejectedConsumer` trên `PaymentService` tiêu thụ sự kiện này, kích hoạt lệnh **`CompensatePayment`**:
    1. Gọi `PaymentRefunder.RefundAsync` để hoàn tiền qua cổng thanh toán với Idempotency Key.
    2. Đảo bút toán trong sổ cái kép: `Ghi Nợ (Debit): TripRevenue`, `Ghi Có (Credit): GatewayClearing`.
    3. Cập nhật trạng thái Payment sang **`Refunded`** kèm lý do từ chối (`RefundReason`).

### 3.6. Strategy Pattern (`MatchingService`)
- Hợp đồng `IPricingStrategy` được triển khai bởi `StandardPricing`, `SurgePricing` (nhân hệ số cao điểm), và `PromoPricing` (Decorator giảm giá khuyến mãi).
- `PricingStrategyFactory` tự động chọn strategy dựa trên mật độ tài xế sẵn sàng trong bán kính 3km quanh điểm đón.

### 3.7. API Gateway & Edge Security (`ApiGateway`)
- Dùng Microsoft YARP (Yet Another Reverse Proxy).
- Xác thực tập trung JWT Bearer thông qua `BuildingBlocks.Security` ngay tại cửa ngõ. Request không có token hợp lệ bị chặn ngay lập tức (HTTP 401).
- Rate Limiting 60 request/phút cho từng Client (dựa trên JWT `sub` hoặc IP).

### 3.8. Resilience Pattern (`BuildingBlocks.Resilience`)
- Đóng gói chính sách độ bền vững dựa trên thư viện **Polly v8**:
  - **Retry Policy với Exponential Backoff & Jitter**: Tự động thử lại khi gặp lỗi tạm thời (HTTP 5xx, 408) với thời gian trễ tăng dần kết hợp độ lệch ngẫu nhiên (jitter) để tránh hiện tượng sập đổ đồng loạt (thundering herd).
  - **Circuit Breaker**: Tự động ngắt mạch khi tỷ lệ lỗi vượt ngưỡng cho phép, chuyển sang trạng thái Open để bảo vệ hệ thống hạ tầng không bị quá tải.

---

## 4. Hệ Thống Quan Sát (Observability & Distributed Tracing)

- Toàn bộ 4 dịch vụ (`ApiGateway`, `TripService`, `MatchingService`, `PaymentService`) đều tích hợp `BuildingBlocks.Observability`.
- Giao thức chuẩn OTLP gRPC xuất toàn bộ Traces, Metrics và Logs về **.NET Aspire Dashboard** (`http://localhost:18888`).
- Header `X-Trace-Id` được tự động trả về cho Client trong mọi phản hồi HTTP để thuận tiện cho việc tra cứu log khi có sự cố.
