using BuildingBlocks.Observability;
using BuildingBlocks.Security;
using TripService.Api.Auth;
using TripService.Api.Endpoints;
using TripService.Api.ExceptionHandling;
using TripService.Application;
using TripService.Application.Common.Interfaces;
using TripService.Infrastructure;
using TripService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddSwiftRideTelemetry("trip-service");

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("TripDb")!,
    builder.Configuration["Services:Matching:BaseUrl"]!,
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

app.MapTripEndpoints();
if (app.Environment.IsDevelopment()) app.MapDevAuthEndpoints(builder.Configuration);

app.Run();