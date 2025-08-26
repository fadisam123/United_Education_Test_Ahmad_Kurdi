using App.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using United_Education_Test_Ahmad_Kurdi;
using United_Education_Test_Ahmad_Kurdi.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

var metricsRoot = new MetricsBuilder()
    .Configuration.Configure(options =>
    {
        options.WithGlobalTags((globalTags, envInfo) =>
        {
            globalTags.Add("app", "United_Education_Test");
        });
    })
    .OutputMetrics.AsPrometheusPlainText()
    .Build();

// register metrics services
builder.Services.AddMetrics(metricsRoot);
builder.Services.AddMetricsEndpoints(); // add endpoint /metrics.

builder.Services.AddControllers();

// Register health checks
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

// Add OpenAPI services for Scalar
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "United Education Test APIs Documentation";
            return Task.CompletedTask;
        });
    });

//// Add Swagger services
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(options =>
//{
//    options.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "United Education Test APIs Documentation",
//        Version = "v1"
//    });
//});

// Register repositories, Sql Server, and seed data (await & async her is just for DB seeding functions)
await builder.Services.AddSQLServerDbContext(builder.Configuration);

// Add etities <=> DTOs mapper services
builder.Services.AddMappingServices();

// Register services and add Redis cache
builder.Services.AddProductCatalogServices(builder.Configuration);

var app = builder.Build();

// Use our middleware EARLY so it wraps the rest of the pipeline.
app.UseRequestLoggingAndMonitoring();

//app.UseMetricsAllMiddleware();
app.UseMetricsEndpoint();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI
    //app.UseSwagger();
    //app.UseSwaggerUI();

    // Enable Scalar UI
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

//app.UseAuthorization();

// Map health and metrics
app.MapHealthChecks("/health"); // expose health endpoint (/health) lightweight live check

app.MapControllers();

app.Run();
