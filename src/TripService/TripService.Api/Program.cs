using BuildingBlocks.Security;
using TripService.Api.Endpoints;
using TripService.Api.ExceptionHandling;
using TripService.Application;
using TripService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("TripDb")!);
builder.Services.AddSwiftRideAuth(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapTripEndpoints();
if (app.Environment.IsDevelopment()) app.MapDevAuthEndpoints(builder.Configuration);

app.Run();