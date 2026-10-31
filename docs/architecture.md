# Kiến Trúc Hệ Thống SwiftRide (Microservices & Clean Architecture)

Tài liệu này mô tả chi tiết kiến trúc phần mềm của dự án **SwiftRide**, bám sát theo tài liệu đặc tả môn học **Kiến trúc Phần mềm - Đại học Sài Gòn (VER 1.0)**.

---

## 1. Cây Thư Mục Chi Tiết Toàn Bộ Hệ Thống (Repository Layout)

```text
D:\Source\SwiftRide
│   .gitignore                                       # Cấu hình bỏ qua bin, obj, .vs, .env, secrets
│   .env.example                                     # Mẫu biến môi trường cho Docker & Production
│   docker-compose.yml                               # Hạ tầng gồm PostgreSQL, MongoDB, RabbitMQ
│   SwiftRide.slnx                                   # Quản trị toàn bộ projects (.NET 10 Solution)
│
├── .github/
│   └── workflows/
│       └── ci.yml                                   # GitHub Actions CI: Build, Test (41 tests) & Docker validate
│
├── docs/
│   ├── run.txt                                      # Hướng dẫn lệnh chạy nhanh
│   ├── architecture.md                              # Tài liệu kiến trúc toàn diện (file này)
│   └── RideLite-KienTrucPhanMem.pdf                 # Đặc tả bài tập lớn
│
├── src/
│   │
│   ├── BuildingBlocks/                              # [THƯ VIỆN DÙNG CHUNG HẠ TẦNG]
│   │   └── BuildingBlocks.Security/                 # Đóng gói xác thực & phân quyền JWT tập trung
│   │       ├── ICurrentUser.cs                      # Abstraction lấy thông tin người dùng đang gọi API
│   │       ├── CurrentUser.cs                       # Hiện thực đọc Id, Role từ HttpContext Claims
│   │       ├── Roles.cs                             # Hằng số vai trò: Rider, Driver, Admin
│   │       ├── Policies.cs                          # Hằng số chính sách: RiderOnly, DriverOnly, AnyRole
│   │       ├── SecurityExtensions.cs                # Extension methods: AddSwiftRideAuth, MapDevAuthEndpoints
│   │       └── BuildingBlocks.Security.csproj
│   │
│   ├── TripService/                                 # [MICROSERVICE 1: QUẢN LÝ CHUYẾN ĐI]
│   │   │                                            # Cơ sở dữ liệu: PostgreSQL (ACID, Concurrency Token xmin)
│   │   ├── TripService.Api/                         # [Presentation Layer]: Minimal API, Middleware
│   │   │   ├── Endpoints/
│   │   │   │   └── TripEndpoints.cs                 # POST /trips, POST /{id}/driver-accept, pickup, dropoff...
│   │   │   ├── ExceptionHandling/
│   │   │   │   └── GlobalExceptionHandler.cs        # Xử lý ngoại lệ chuẩn ProblemDetails (400, 403, 404, 409)
│   │   │   ├── Properties/launchSettings.json       # Port: 5130 (HTTP)
│   │   │   ├── appsettings.Development.json
│   │   │   ├── appsettings.json                     # Chuỗi kết nối PostgreSQL (TripDb) & JWT config
│   │   │   ├── Program.cs                           # Composition Root: DI & Middleware Pipeline
│   │   │   └── TripService.Api.csproj
│   │   │
│   │   ├── TripService.Application/                 # [Application Layer]: Use Cases & CQRS Handlers
│   │   │   ├── Common/
│   │   │   │   ├── Exceptions/                      # ConcurrencyException, ForbiddenException, NotFoundException
│   │   │   │   └── Interfaces/                      # IHandler<TReq, TRes>, IUnitOfWork
│   │   │   ├── Trips/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── AcceptTrip/                  # AcceptTripCommand + AcceptTripHandler
│   │   │   │   │   ├── DropOffTrip/                 # DropOffTripCommand + DropOffTripHandler
│   │   │   │   │   ├── PickUpTrip/                  # PickUpTripCommand + PickUpTripHandler
│   │   │   │   │   └── RequestTrip/                 # RequestTripCommand + RequestTripHandler
│   │   │   │   ├── Dtos/                            # TripDto, TripMappings
│   │   │   │   ├── Interfaces/                      # ITripRepository, TripRepositoryExtensions
│   │   │   │   └── Queries/                         # GetTripQuery + GetTripHandler
│   │   │   ├── DependencyInjection.cs               # Tự động quét và đăng ký mọi IHandler<,>
│   │   │   └── TripService.Application.csproj
│   │   │
│   │   ├── TripService.Domain/                      # [Domain Layer]: Pure C# (Rich Domain Model)
│   │   │   ├── Common/
│   │   │   │   ├── DomainException.cs               # Lỗi vi phạm trạng thái nghiệp vụ
│   │   │   │   └── DomainValidationException.cs     # Lỗi vi phạm quy tắc dữ liệu đầu vào
│   │   │   ├── Trips/
│   │   │   │   ├── Trip.cs                          # Aggregate Root: Encapsulation & Transition logic
│   │   │   │   └── TripStatus.cs                    # Enum: Requested, DriverAccepted, PickedUp, DroppedOff, Cancelled
│   │   │   └── TripService.Domain.csproj
│   │   │
│   │   └── TripService.Infrastructure/              # [Infrastructure Layer]: Data Access & Persistence
│   │       ├── Persistence/
│   │       │   ├── Configurations/
│   │       │   │   └── TripConfiguration.cs         # Fluent API cấu hình bảng Trips & Concurrency Token (xmin)
│   │       │   ├── Migrations/                      # EF Core code-first migrations
│   │       │   ├── Repositories/
│   │       │   │   └── TripRepository.cs            # Hiện thực ITripRepository với EF Core DbContext
│   │       │   └── TripDbContext.cs                 # DbContext & Unit of Work implementation
│   │       ├── DependencyInjection.cs               # Đăng ký DbContext, Repository, UnitOfWork
│   │       └── TripService.Infrastructure.csproj
│   │
│   └── MatchingService/                             # [MICROSERVICE 2: GHÉP XE & TÍNH GIÁ]
│       │                                            # Cơ sở dữ liệu: MongoDB (Geo-spatial 2dsphere index)
│       ├── MatchingService.Api/                     # [Presentation Layer]: Minimal API
│       │   ├── Endpoints/
│       │   │   ├── DriverEndpoints.cs               # PUT /drivers/me/location, POST /match
│       │   │   └── PricingEndpoints.cs              # POST /quote (RiderOnly)
│       │   ├── ExceptionHandling/
│       │   │   └── GlobalExceptionHandler.cs
│       │   ├── Properties/launchSettings.json       # Port: 5197 (HTTP)
│       │   ├── appsettings.Development.json
│       │   ├── appsettings.json                     # Kết nối MongoDB (MatchDb) & Đơn giá cước
│       │   ├── Program.cs
│       │   └── MatchingService.Api.csproj
│       │
│       ├── MatchingService.Application/             # [Application Layer]: Use Cases & Matching Rules
│       │   ├── Common/
│       │   │   └── Interfaces/                      # IHandler<TReq, TRes>
│       │   ├── Drivers/
│       │   │   ├── Commands/UpdateDriverLocation/   # Cập nhật vị trí tài xế từ token đăng nhập
│       │   │   ├── Dtos/                            # DriverDtos, NearbyDriverDto
│       │   │   ├── Interfaces/                      # IDriverLocationRepository
│       │   │   ├── Queries/MatchDrivers/            # Tìm tài xế gần nhất quanh điểm đón
│       │   │   └── MatchingPolicy.cs                # Bán kính tìm kiếm, thời hạn vị trí (TTL)
│       │   ├── Pricing/
│       │   │   ├── Dtos/                            # QuoteDto
│       │   │   └── Queries/GetQuote/                # Ước lượng quãng đường, tính surge & báo giá
│       │   ├── DependencyInjection.cs
│       │   └── MatchingService.Application.csproj
│       │
│       ├── MatchingService.Domain/                  # [Domain Layer]: Pricing Strategies & Geo Models
│       │   ├── Common/                              # DomainValidationException, DomainException
│       │   ├── Drivers/                             # DriverLocation entity
│       │   ├── Geo/
│       │   │   └── GeoPoint.cs                      # Value Object tính khoảng cách Haversine & validate tọa độ
│       │   ├── Pricing/
│       │   │   ├── Strategies/                      # Strategy Pattern:
│       │   │   │   ├── IPricingStrategy.cs          # Hợp đồng chiến lược giá
│       │   │   │   ├── StandardPricing.cs           # Giá chuẩn (hệ số s=1.0)
│       │   │   │   ├── SurgePricing.cs              # Giá cao điểm (hệ số s=1.3, s=1.8)
│       │   │   │   └── PromoPricing.cs              # Decorator giảm giá khuyến mãi (η)
│       │   │   ├── FareCalculator.cs                # Tính giá theo công thức đặc tả mục 2.3
│       │   │   ├── FareModels.cs                    # FareRates, FareInput, FareBreakdown
│       │   │   ├── PricingStrategyFactory.cs        # Factory tự chọn Strategy theo số tài xế rảnh
│       │   │   └── RouteEstimator.cs                # Ước lượng khoảng cách & thời gian di chuyển
│       │   └── MatchingService.Domain.csproj
│       │
│       └── MatchingService.Infrastructure/          # [Infrastructure Layer]: MongoDB Driver
│           ├── Persistence/
│           │   ├── Documents/                       # DriverLocationDocument (GeoJsonPoint)
│           │   ├── Repositories/                    # DriverLocationRepository ($near, 2dsphere query)
│           │   └── MatchingMongoContext.cs          # Đảm bảo MongoDB collection & Geo Index
│           ├── DependencyInjection.cs
│           └── MatchingService.Infrastructure.csproj
│
└── tests/
    ├── TripService.Tests/                           # Unit Tests & Architecture Tests cho TripService
    │   ├── ApplicationTests/HandlerTests.cs         # Test CQRS Command / Query Handlers (Moq)
    │   ├── Architecture/DependencyRulesTests.cs     # Tự động khóa Clean Architecture (NetArchTest)
    │   ├── DomainTests/TripTests.cs                 # Test trạng thái Aggregate Root Trip
    │   └── TripService.Tests.csproj
    │
    └── MatchingService.Tests/                       # Unit Tests & Architecture Tests cho MatchingService
        ├── DependencyRulesTests.cs                  # Khóa kiến trúc tầng độc lập cho MatchingService
        ├── PricingAndGeoTests.cs                    # Test công thức tính giá, Strategy, Haversine, Surge
        └── MatchingService.Tests.csproj
```

---

## 2. Đánh Giá Độ Chuẩn Clean Architecture & Nguyên Tắc SOLID

### 2.1. Clean Architecture (Độc Lập Tầng)
Cả hai Microservice đều tuân thủ nghiêm ngặt mô hình củ hành (Onion / Clean Architecture):
1. **Domain Layer (Trung tâm)**:
   - `TripService.Domain`: Chứa Aggregate Root `Trip`, bảo vệ bất biến trạng thái qua các hàm nghiệp vụ (`Request`, `AcceptBy`, `PickUp`, `DropOff`). Không chứa bất kỳ thư viện bên ngoài hay framework nào.
   - `MatchingService.Domain`: Chứa Value Object `GeoPoint` (tự tính khoảng cách theo công thức Haversine), `FareCalculator` và các Chiến lược giá (`IPricingStrategy`).
2. **Application Layer**:
   - Chứa toàn bộ Use Cases dưới dạng CQRS (`Commands/` cho ghi, `Queries/` cho đọc).
   - Chỉ phụ thuộc vào `Domain` và thư viện trừu tượng `BuildingBlocks.Security`.
   - Hoàn toàn không biết Database cụ thể là PostgreSQL hay MongoDB.
3. **Infrastructure Layer**:
   - Hiện thực hóa các Repository interfaces được định nghĩa từ Application (`ITripRepository`, `IDriverLocationRepository`).
   - Sử dụng EF Core với PostgreSQL (TripDb) và MongoDB C# Driver với Geo-spatial index (MatchDb).
4. **Api Layer (Composition Root)**:
   - Khởi tạo DI container, cấu hình Exception Handling và map các endpoints RESTful bằng ASP.NET Core Minimal APIs.
5. **Kiểm thử kiến trúc tự động (Architecture Tests)**:
   - Cả hai bộ test đều dùng `NetArchTest.Rules` để khẳng định Domain không bao giờ được phụ thuộc Application/Infrastructure/Api, bảo vệ codebase khỏi việc vi phạm kiến trúc trong tương lai.

### 2.2. Áp Dụng SOLID và Design Patterns
- **S – Single Responsibility**: `FareCalculator` chỉ chịu trách nhiệm tính toán giá, không quan tâm việc lưu dữ liệu hay gửi tin nhắn.
- **O – Open/Closed**: Khi muốn thêm một loại giá mới (ví dụ `NightTimePricing`), chỉ cần viết class mới implement `IPricingStrategy` mà không phải sửa `FareCalculator` hay `StandardPricing`.
- **L – Liskov Substitution**: Mọi chiến lược triển khai `IPricingStrategy` đều bảo đảm trả về hệ số hợp lệ, giá tiền sau tính toán luôn không âm.
- **I – Interface Segregation**: Các interface repository (`ITripRepository`, `IDriverLocationRepository`, `IUnitOfWork`) được thiết kế tinh gọn, chỉ chứa các phương thức thực sự cần thiết.
- **D – Dependency Inversion**: Các Command/Query Handler phụ thuộc vào interface trừu tượng (`ITripRepository`, `ICurrentUser`), việc bind implementation do Dependency Injection đảm nhiệm.

---

## 3. Kiến Trúc Xác Thực (Authentication) & Bảo Mật

### 3.1. Tại sao cần `BuildingBlocks.Security`?
Trong hệ thống Microservices, việc copy-paste code cấu hình JWT, Claims, Roles và Policies giữa các service vi phạm nguyên tắc **DRY** (Don't Repeat Yourself) và gây ra rủi ro sai lệch cấu hình:
- Khác biệt Issuer/Audience giữa các service khiến token được tạo từ service này bị service kia từ chối.
- Khi cần cập nhật thêm Role mới (ví dụ `Admin`, `Support`, `Dispatcher`), lập trình viên phải sửa đổi ở nhiều nơi.

**Giải pháp**:
`BuildingBlocks.Security` đóng gói:
1. `AddSwiftRideAuth(IConfiguration)`: Cấu hình tập trung JWT Bearer, Signature Key, Issuer, Audience và các Policies:
   - `RiderOnly`: Chỉ cho phép người dùng có Role `Rider`.
   - `DriverOnly`: Chỉ cho phép người dùng có Role `Driver`.
   - `AnyRole`: Cho phép tất cả người dùng hợp lệ.
2. `ICurrentUser` & `CurrentUser`: Tự động đọc ID người dùng và Role từ Claims của `HttpContext.User`.
3. `MapDevAuthEndpoints(IConfiguration)`: Tạo endpoint `/dev/token` phục vụ test nhanh ở môi trường Development, có thể sinh token cho bất kỳ role nào.

---

## 4. Chiến Lược Quản Lý Secret (Secret Management)

Tuyệt đối **KHÔNG** lưu trữ thông tin nhạy cảm (Private Key JWT, Password Database, API Key bên thứ 3) trực tiếp vào file mã nguồn hoặc file `appsettings.json` được commit lên Git.

SwiftRide áp dụng chiến lược quản lý Secret theo 3 cấp độ:

### Cấp độ 1: Môi trường Lập trình Cục bộ (Local Development)
Sử dụng công cụ **.NET Secret Manager (`dotnet user-secrets`)**:
- Thông tin secret được lưu trữ tại máy cá nhân của lập trình viên (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` trên Windows, `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` trên Linux/macOS).
- File này hoàn toàn nằm ngoài thư mục source code, không thể bị commit nhầm lên Git.
- Lệnh thiết lập:
  ```bash
  # Thiết lập khóa JWT cho TripService.Api
  dotnet user-secrets set "Jwt:Key" "dev-only-secret-key-change-me-at-least-32-chars!" --project src/TripService/TripService.Api

  # Thiết lập khóa JWT cho MatchingService.Api
  dotnet user-secrets set "Jwt:Key" "dev-only-secret-key-change-me-at-least-32-chars!" --project src/MatchingService/MatchingService.Api
  ```

### Cấp độ 2: Chạy Môi Trường Container Cục bộ (Docker Compose)
Sử dụng file **`.env`** (kèm file mẫu `.env.example`):
- Docker Compose sẽ tự động đọc các biến trong file `.env` để truyền vào các container.
- File `.env` **đã được cấu hình trong `.gitignore`**.
- Lập trình viên mới chỉ cần:
  ```bash
  cp .env.example .env
  ```
  Sau đó chỉnh sửa mật khẩu nếu muốn và chạy `docker compose up -d`.

### Cấp độ 3: Môi trường Kiểm thử / Triển khai Thực tế (Staging & Production)
- **Environment Variables**: Nạp secret qua biến môi trường của hệ thống host hoặc CI/CD pipeline (GitHub Actions Secrets, GitLab CI Variables).
  - Tên biến dạng: `Jwt__Key`, `ConnectionStrings__TripDb`, `ConnectionStrings__MatchDb`.
- **Cloud Secret Stores**: Tích hợp các dịch vụ quản lý secret chuyên dụng:
  - **Azure Key Vault** (với package `Azure.Extensions.AspNetCore.Configuration.Secrets`)
  - **AWS Secrets Manager**
  - **HashiCorp Vault**

---

## 5. Hướng Dẫn Vận Hành & Khởi Chạy (Quick Start)

### Bước 1: Khởi động Hạ tầng Docker (PostgreSQL, MongoDB, RabbitMQ)
Chạy lệnh tại thư mục gốc:
```bash
docker compose up -d
```
Kiểm tra các dịch vụ đã sẵn sàng:
- **PostgreSQL**: `localhost:5432` (User: `swiftride`, Database: `tripdb`)
- **MongoDB**: `localhost:27017`
- **RabbitMQ Dashboard**: Truy cập giao diện web tại `http://localhost:15672` (User: `guest`, Password: `guest`)

### Bước 2: Cập nhật Cơ sở dữ liệu EF Core cho TripService
```bash
dotnet ef database update --project src/TripService/TripService.Infrastructure --startup-project src/TripService/TripService.Api
```

### Bước 3: Chạy Kiểm Thử (Tất cả 41 Tests)
```bash
dotnet test
```

### Bước 4: Chạy Các Microservices
Mở 2 terminal riêng biệt:
- **Terminal 1 (TripService)**:
  ```bash
  dotnet run --project src/TripService/TripService.Api
  ```
  API hoạt động tại: `http://localhost:5130`
- **Terminal 2 (MatchingService)**:
  ```bash
  dotnet run --project src/MatchingService/MatchingService.Api
  ```
  API hoạt động tại: `http://localhost:5197`

### Bước 5: Kiểm Thử Tạo Token & Gọi API
1. **Lấy Token cho Rider**:
   ```bash
   curl -X POST http://localhost:5130/dev/token \
     -H "Content-Type: application/json" \
     -d '{"role":"Rider"}'
   ```
2. **Lấy Token cho Driver**:
   ```bash
   curl -X POST http://localhost:5197/dev/token \
     -H "Content-Type: application/json" \
     -d '{"role":"Driver"}'
   ```
3. **Dùng Token gọi API**: Gắn Header `Authorization: Bearer <TOKEN>` khi gọi các endpoint `/trips`, `/quote`, `/match` hoặc `/drivers/me/location`.

---

## 6. Đường Ống CI/CD Tự Động (GitHub Actions Pipeline)

Dự án được trang bị luồng kiểm thử tự động tại [.github/workflows/ci.yml](.github/workflows/ci.yml).

### Các kích hoạt (Triggers):
- Mỗi khi `git push` lên nhánh `main` hoặc bất kỳ nhánh `feature/**`.
- Mỗi khi mở `pull_request` vào nhánh `main`.

### Các giai đoạn (Jobs):
1. **`build-and-test`**:
   - Cài đặt .NET 10 SDK (`actions/setup-dotnet@v4`).
   - Khôi phục NuGet packages (`dotnet restore SwiftRide.slnx`).
   - Biên dịch cấu hình Release (`dotnet build --no-restore --configuration Release`).
   - Chạy toàn bộ Unit Tests & Architecture Tests (`dotnet test`), đồng thời thu thập độ bao phủ mã nguồn (Code Coverage).
   - Xuất kết quả kiểm thử dạng Artifact (`test-results`).
2. **`docker-validate`**:
   - Nạp file cấu hình môi trường mẫu `.env.example`.
   - Chạy lệnh `docker compose config` để bảo đảm file `docker-compose.yml` không bị sai cú pháp hoặc thiếu biến môi trường.

