using United_Education_Test_Ahmad_Kurdi.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "United Education_Test APIs Documentation";
            //document.Info.Description = "Desc";
            document.Info.Version = "v1";
            return Task.CompletedTask;
        });
    });

// Register health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());

// OpenTelemetry setup
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("RequestLoggingAndMonitoring"))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter("RequestLoggingAndMonitoring.Middleware")
            .AddPrometheusExporter();
    });


var app = builder.Build();


// Use our middleware EARLY so it wraps the rest of the pipeline.
app.UseRequestLoggingAndMonitoring();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Make the API documentation available on development only
}

app.UseHttpsRedirection();

//app.UseAuthorization();

// Map health and metrics
app.MapHealthChecks("/health");      // expose health endpoint (/health) lightweight live check
app.MapPrometheusScrapingEndpoint(); // expose metrics endpoint (/metrics)

app.MapControllers();

app.Run();
