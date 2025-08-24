namespace United_Education_Test_Ahmad_Kurdi.Infrastructure.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLoggingAndMonitoring(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingAndMonitoringMiddleware>();
        }
    }
}
