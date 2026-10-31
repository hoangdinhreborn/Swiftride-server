using BuildingBlocks.Observability;
using BuildingBlocks.Security;
using PaymentService.Api.Auth;
using PaymentService.Api.Endpoints;
using PaymentService.Api.ExceptionHandling;
using PaymentService.Application;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments;
using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddSwiftRideTelemetry("payment-service");

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddSingleton(builder.Configuration.GetSection("AutoPay").Get<AutoPayOptions>() ?? new AutoPayOptions());
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("PayDb")!,
    builder.Configuration["Services:Trip:BaseUrl"]!,
    builder.Configuration["RabbitMq:Uri"]!);

builder.Services.AddSwiftRideAuth(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAccessTokenProvider, HttpContextAccessTokenProvider>();

var app = builder.Build();
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.ApplyMigrationsAsync();

app.UseTraceIdHeader();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapPaymentEndpoints();
if (app.Environment.IsDevelopment()) app.MapDevAuthEndpoints(builder.Configuration);

app.Run();