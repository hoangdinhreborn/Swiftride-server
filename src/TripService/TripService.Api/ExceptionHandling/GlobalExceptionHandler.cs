using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TripService.Application.Common.Exceptions;
using TripService.Domain.Common;

namespace TripService.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            DomainValidationException => (400, "Dữ liệu không hợp lệ"),
            NotFoundException => (404, "Không tìm thấy"),
            ForbiddenException => (403, "Không có quyền thực hiện"),
            ConcurrencyException => (409, "Xung đột dữ liệu"),
            PricingUnavailableException => (503, "Dịch vụ báo giá không khả dụng"),
            BadHttpRequestException => (400, "Yêu cầu không hợp lệ"),
            DomainException => (409, "Sai trạng thái nghiệp vụ"),
            _ => (500, "")
        };

        if (status == 500) return false;   // lỗi lạ: để framework trả 500

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = ex.Message }, ct);
        return true;
    }
}