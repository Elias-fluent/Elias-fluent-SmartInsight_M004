using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SmartInsight.API.Extensions;
using SmartInsight.API.Security;
using SmartInsight.Telemetry.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilogConfig();

// Add services to the container
builder.Services.AddControllers();

// Configure API versioning
builder.Services.AddApiVersioningConfiguration();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwtSupport();

// Configure JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Configure rate limiting
builder.Services.AddRateLimiting();

// Add telemetry services
builder.Services.AddTelemetryServices(builder.Configuration);

var app = builder.Build();

// Use Serilog request logging
app.UseSerilogRequestLogging();

// Configure Serilog with the application services
app.UseSerilogConfiguration(app.Configuration, app.Environment);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// Apply rate limiting middleware
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
