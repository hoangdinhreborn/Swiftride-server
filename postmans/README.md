# Hướng Dẫn Sử Dụng Postman Demo SwiftRide Microservices

Thư mục này chứa bộ tài nguyên **Postman Collection & Environment** chuẩn mực để phục vụ thuyết trình, báo cáo và demo đồ án môn học **Kiến trúc Phần mềm (SwiftRide)**.

---

## 1. Danh Sách File

1. **`SwiftRide_Comprehensive.postman_collection.json`**:
   - Collection tích hợp toàn diện gồm **3 Folders** chính, bao quát 100% các yêu cầu nghiệp vụ và kỹ thuật theo đặc tả.
   - Tích hợp sẵn mã **Postman Scripts (`Tests`)** để tự động parse response và gán giá trị vào biến môi trường / collection variables (không cần copy-paste token hay ID thủ công).
2. **`SwiftRide.postman_environment.json`**:
   - File môi trường Postman chứa biến `baseUrl = http://localhost:5000` (qua API Gateway) và các biến tự động cập nhật (`riderToken`, `driverTokenA`, `driverTokenB`, `adminToken`, `tripId`, `paymentId`).

---

## 2. Cấu Trúc 3 Folders Trong Collection

### 📁 Folder 1: `1. Authentication & Security`
Cấp phát token JWT Bearer qua endpoint `/dev/token` của API Gateway:
- **1.1 Get Rider Token**: Sinh token cho Khách hàng (`role = Rider`). Script tự động set vào biến `riderToken` & `token`.
- **1.2 Get Driver 1 Token (Driver A)**: Sinh token cho Tài xế A (`role = Driver`). Script tự động set vào biến `driverTokenA`.
- **1.3 Get Driver 2 Token (Driver B)**: Sinh token cho Tài xế B (`role = Driver`). Script tự động set vào biến `driverTokenB`.
- **1.4 Get Admin Token**: Sinh token cho Quản trị viên (`role = Admin`). Script tự động set vào biến `adminToken`.

---

### 📁 Folder 2: `2. Trip Lifecycle & Driver Dispatching`
Demo toàn bộ vòng đời chuyến đi theo **State Pattern (GoF)** và **Lưu đồ điều phối tài xế (Flowchart)**:
- **2.1 Update Driver A Location**: Tài xế A bật Online tại Chợ Bến Thành (`10.7725, 106.6990`).
- **2.2 Update Driver B Location**: Tài xế B bật Online tại vị trí dự phòng lân cận (`10.7735, 106.7000`).
- **2.3 Get Fare Quote**: Khách hàng tra cứu báo giá cước ước tính (thử nghiệm Strategy Pattern).
- **2.4 Request Trip**: Rider đặt xe $\rightarrow$ Chuyến đi khởi tạo ở trạng thái `Requested`. **Script tự động lưu `tripId`**.
- **2.5 Confirm Trip**: Rider đồng ý giá cước $\rightarrow$ `DriverDispatcher` tự động điều phối cho Tài xế A gần nhất $\rightarrow$ Trạng thái chuyển thành `DriverOffered`.
- **2.6 Check Driver A Offers**: Tài xế A gọi `GET /trips/offers` và nhận được cuốc xe đề xuất.
- **2.7 Driver A Decline**: Tài xế A từ chối cuốc xe $\rightarrow$ Hệ thống tự động chuyển đề xuất sang Tài xế B.
- **2.8 Check Driver B Offers**: Tài xế B kiểm tra và thấy cuốc xe vừa chuyển sang cho mình.
- **2.9 Driver B Accept Trip**: Tài xế B nhận cuốc $\rightarrow$ Trạng thái chuyển sang `DriverAccepted` (Phát sự kiện `TripAccepted` vào Outbox).
- **2.10 Driver B Start En Route**: Tài xế B báo đang trên đường đến đón khách $\rightarrow$ Chuyển sang `EnRoute`.
- **2.11 Driver B Pick Up**: Tài xế đón khách lên xe $\rightarrow$ Chuyển sang `PickedUp`.
- **2.12 Driver B Drop Off**: Tài xế trả khách tại điểm đến $\rightarrow$ Chuyển sang `DroppedOff` và ghi nhận sự kiện `RideCompleted` vào Transactional Outbox (kích hoạt RabbitMQ Saga thanh toán tự động).
- **2.13 Get Trip Status**: Kiểm tra trạng thái chuyến đi $\rightarrow$ Sau khi PaymentService trừ tiền thành công, trạng thái tự động chuyển thành **`Paid`**.
- **2.14 Cancel Trip**: Kịch bản phụ: Rider từ chối báo giá hoặc hủy chuyến $\rightarrow$ Chuyển sang `Cancelled`.

---

### 📁 Folder 3: `3. Payment, Ledger & Gateway Aggregation`
Demo hệ thống thanh toán độc lập, sổ cái kế toán và tối ưu hóa cổng API Gateway:
- **3.1 Create Payment**: Thanh toán thủ công với **Idempotency Key (`{{$guid}}`)**, ghi nhận bút toán Nợ/Có vào Sổ cái kép (`Double-Entry Ledger`).
- **3.2 Get Payment By Trip ID**: Tra cứu thông tin thanh toán và các dòng bút toán Nợ/Có chi tiết theo mã chuyến đi.
- **3.3 Get Aggregated Trip**: Hiện thực **API Gateway Aggregation Pattern** (`GET /aggregate/trips/{id}`). Gọi song song `TripService` và `PaymentService`, ghép kết quả trả về trong **1 request duy nhất** `{ trip, payment, paymentState }`.
- **3.4 Refund Payment**: Quản trị viên hoàn tiền hoặc Saga tự động bồi thường khi chuyến đi bị từ chối $\rightarrow$ Đảo bút toán sổ cái sang `Refunded`.

---

## 3. Hướng Dẫn Sử Dụng Trong Postman

1. Mở ứng dụng **Postman**.
2. Bấm nút **Import** (góc trên bên trái).
3. Kéo thả cả 2 file:
   - `postmans/SwiftRide_Comprehensive.postman_collection.json`
   - `postmans/SwiftRide.postman_environment.json`
4. Ở góc trên bên phải của Postman, chọn Environment là **`SwiftRide Local Environment`**.
5. Thực hiện chạy lần lượt các request theo thứ tự đánh số:
   - Chạy các request trong **Folder 1** để lấy token (token sẽ tự động điền vào biến môi trường).
   - Chạy tuần tự các request từ `2.1` đến `2.13` trong **Folder 2** để xem chuyến đi chuyển đổi trạng thái mượt mà từ lúc đặt xe đến khi hoàn tất và thanh toán.
   - Chạy các request trong **Folder 3** để xem dữ liệu Sổ cái kép và API Gateway Aggregation.
