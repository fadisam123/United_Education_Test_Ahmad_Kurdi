using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Registry;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using United_Education_Test_Ahmad_Kurdi.DTOs.Response;

public class RequestLoggingAndMonitoringMiddleware
{
    #region Fields
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingAndMonitoringMiddleware> _logger;
    private readonly IMetrics _metrics;
    public static readonly CounterOptions HttpErrors = new CounterOptions
    {
        Name = "http.errors",
        Context = "http",
        MeasurementUnit = Unit.Errors
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Defining some const
    private const string ErrorMessage = "An unexpected error occurred";
    private const string HealthPath = "/health";
    private const string MetricPath = "/metrics";
    #endregion

    #region Constructor
    public RequestLoggingAndMonitoringMiddleware(
        RequestDelegate next,
        IMetrics metrics,
        ILogger<RequestLoggingAndMonitoringMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    #endregion

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var path = context.Request.Path;

        var isHealthPath = path.StartsWithSegments(HealthPath, StringComparison.OrdinalIgnoreCase);
        var isMetricPath = path.StartsWithSegments(MetricPath, StringComparison.OrdinalIgnoreCase);

        // Check if path should be ignored
        if (isHealthPath || isMetricPath)
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        var correlationId = GetOrCreateCorrelationId(context);
        SetCorrelationIdInResponse(context, correlationId);

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = path,
            ["RequestMethod"] = method
        }))
        {
            var sw = Stopwatch.StartNew();
            Exception? capturedException = null;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                capturedException = ex;
                await HandleExceptionAsync(context, ex, correlationId, sw.Elapsed);
                // Count as error
                TrackError(500, context);
            }
            finally
            {
                sw.Stop();
                if (capturedException is null)
                {
                    LogRequestInfo(context, sw.Elapsed, correlationId);
                    if (context.Response.StatusCode >= 400)
                        TrackError(context.Response.StatusCode, context);
                }
            }
        }
    }

    #region Private Methods
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check header if Correlation ID exist
        var header = "X-Correlation-ID";
        if (context.Request.Headers.TryGetValue(header, out var value))
        {
            var correlationId = value.ToString();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                context.Items["CorrelationId"] = correlationId;
                return correlationId;
            }
        }

        // Generate new ID if not found
        var newId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = newId;
        return newId;
    }

    private static void SetCorrelationIdInResponse(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
            }
            return Task.CompletedTask;
        });
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string correlationId,
        TimeSpan elapsed)
    {
        _logger.LogError(exception,
            "Unhandled exception for {Method} {Path} after {ElapsedMs:F2} ms (CorrelationId: {CorrelationId})",
            context.Request.Method,
            context.Request.Path.Value,
            elapsed.TotalMilliseconds,
            correlationId);

        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";

            var errorResponse = ApiErrorResponse.Create(ErrorMessage, "SERVER_ERROR", correlationId);

            var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
            await context.Response.WriteAsync(json, context.RequestAborted);
        }
        else
        {
            // Response already started, can't modify it
            _logger.LogWarning(
                "Cannot write error response - response has already started (CorrelationId: {CorrelationId})",
                correlationId);
        }
    }

    private void TrackError(int statusCode, HttpContext context)
    {
        var method = context.Request.Method ?? "UNKNOWN";
        var path = context.Request.Path.Value ?? "UNKNOWN";
        var status = statusCode.ToString();

        // Use MetricTags with arrays (safe and explicit)
        var keys = new[] { "method", "path", "status" };
        var values = new[] { method, path, status ?? "UNKNOWN" };
        var tags = new MetricTags(keys, values);

        _metrics.Measure.Counter.Increment(HttpErrors, tags);
    }

    private static LogLevel DetermineLogLevel(int statusCode) => statusCode switch
    {
        >= 500 => LogLevel.Error,
        >= 400 => LogLevel.Warning,
        _ => LogLevel.Information
    };

    private void LogRequestInfo(HttpContext context, TimeSpan elapsed, string correlationId)
    {
        var statusCode = context.Response.StatusCode;
        var logLevel = DetermineLogLevel(statusCode);

        _logger.Log(logLevel,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F2} ms (CorrelationId: {CorrelationId})",
            context.Request.Method,
            context.Request.Path.Value,
            statusCode,
            elapsed.TotalMilliseconds,
            correlationId);
    }
    #endregion
}