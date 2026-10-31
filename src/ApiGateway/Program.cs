using System.Security.Claims;
using System.Threading.RateLimiting;
using BuildingBlocks.Observability;
using BuildingBlocks.Security;

var builder = WebApplication.CreateBuilder(args);
builder.AddSwiftRideTelemetry("api-gateway"); 

// Xác thực JWT ở cửa vào (dùng chung cấu hình với các service)
builder.Services.AddSwiftRideAuth(builder.Configuration);

// Rate limit: 60 request/phút cho mỗi user (chưa đăng nhập thì tính theo IP)
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("perClient", ctx =>
    {
        var key = ctx.User.FindFirstValue("sub")
                  ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? ctx.Connection.RemoteIpAddress?.ToString()
                  ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseTraceIdHeader();  
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapReverseProxy();

app.Run();