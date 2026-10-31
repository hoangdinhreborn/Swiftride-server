using MatchingService.Api.Auth;
using MatchingService.Api.Endpoints;
using MatchingService.Api.ExceptionHandling;
using MatchingService.Application;
using MatchingService.Domain.Pricing;
using MatchingService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton(builder.Configuration.GetSection("Fare").Get<FareRates>() ?? new FareRates());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("MatchDb")!,
    builder.Configuration["MongoDb:Database"]!);
builder.Services.AddApiAuth(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapDriverEndpoints();
app.MapPricingEndpoints();
if (app.Environment.IsDevelopment()) app.MapDevEndpoints();

app.Run();