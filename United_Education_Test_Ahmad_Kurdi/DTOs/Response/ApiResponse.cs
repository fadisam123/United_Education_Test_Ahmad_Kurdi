namespace United_Education_Test_Ahmad_Kurdi.DTOs.Response
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; } = true;
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        public static ApiResponse<T> Scucces(T data, string message = "Operation completed successfully")
        {
            return new() { Message = message, Data = data };
        }

    }
}
