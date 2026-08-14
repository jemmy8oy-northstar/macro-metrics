using Scalar.AspNetCore;
using MacroMetrics.WebApi;
using MacroMetrics.WebApi.Routes;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBackendServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/v1");
}

app.UsePathBase("/macro-metrics");
app.UseHttpsRedirection();

app.MapGroup("/api")
    .MapStatusRoutes()
    .MapMetricsRoutes()
    .WithOpenApi();

app.Run();
