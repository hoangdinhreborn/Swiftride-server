using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Domain.Common;

namespace PaymentService.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            DomainValidationException      => (400, "Dữ liệu không hợp lệ"),
            BadHttpRequestException        => (400, "Yêu cầu không hợp lệ"),
            NotFoundException              => (404, "Không tìm thấy"),
            ForbiddenException             => (403, "Không có quyền thực hiện"),
            ConcurrencyException           => (409, "Xung đột dữ liệu"),
            DependencyUnavailableException => (503, "Dịch vụ phụ thuộc không khả dụng"),
            DomainException                => (409, "Sai trạng thái nghiệp vụ"),
            _                              => (500, "")
        };

        if (status == 500) return false;

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = ex.Message }, ct);
        return true;
    }
}