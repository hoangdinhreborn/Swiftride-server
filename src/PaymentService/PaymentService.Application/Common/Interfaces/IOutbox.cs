namespace PaymentService.Application.Common.Interfaces;

// Ghi sự kiện vào cùng transaction với dữ liệu nghiệp vụ. Chỉ thực sự lưu khi gọi IUnitOfWork.SaveChangesAsync
public interface IOutbox
{
    void Enqueue(string routingKey, object message);
}