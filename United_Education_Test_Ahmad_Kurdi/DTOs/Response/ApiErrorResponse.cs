namespace United_Education_Test_Ahmad_Kurdi.DTOs.Response
{
    public class ApiErrorResponse
    {
        public bool Success { get; init; } = false;
        public string Error { get; init; } = string.Empty;
        public string ErrorCode { get; init; } = string.Empty;
        public string CorrelationId { get; init; } = string.Empty;
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public IEnumerable<string>? Details { get; init; }

        public static ApiErrorResponse Create(string error, string errorCode, string correlationId, IEnumerable<string>? details = null) =>
            new()
            {
                Error = error,
                ErrorCode = errorCode,
                CorrelationId = correlationId,
                Details = details
            };
    }
}
