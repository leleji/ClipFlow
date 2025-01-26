namespace ClipFlow.Models
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Code = 200,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Error(int code, string message)
        {
            return new ApiResponse<T>
            {
                Code = code,
                Message = message,
                Data = default
            };
        }

        public bool IsSuccessStatusCode { get { return Code==200; } }
    }
} 