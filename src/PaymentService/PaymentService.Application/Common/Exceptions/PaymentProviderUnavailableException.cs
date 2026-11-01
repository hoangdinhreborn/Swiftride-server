namespace PaymentService.Application.Common.Exceptions;

// Lỗi TẠM THỜI của cổng thanh toán (timeout, mất kết nối, 5xx): thử lại được.
// Thẻ bị từ chối KHÔNG phải loại này, đó là ChargeResult với Approved = false
public class PaymentProviderUnavailableException(string message) : Exception(message);