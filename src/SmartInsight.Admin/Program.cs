using SmartInsight.Telemetry.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilogConfig();

// Add telemetry services
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment.EnvironmentName);

var app = builder.Build();

// Use Serilog request logging
app.UseSerilogRequestLogging();

// Configure Serilog with the application services
app.UseSerilogConfiguration(app.Configuration, app.Environment);

app.MapGet("/", () => "Hello World!");

app.Run();
