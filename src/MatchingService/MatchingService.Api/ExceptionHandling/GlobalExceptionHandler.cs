using MatchingService.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MatchingService.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            DomainValidationException => (400, "Dữ liệu không hợp lệ"),
            BadHttpRequestException   => (400, "Yêu cầu không hợp lệ"),
            DomainException           => (409, "Sai nghiệp vụ"),
            _                         => (500, "")
        };

        if (status == 500) return false;

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = ex.Message }, ct);
        return true;
    }
}