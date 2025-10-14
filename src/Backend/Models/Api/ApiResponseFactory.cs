namespace AutoBot.Models.Api;

public static class ApiResponseFactory
{
    public static ApiResponse<T> CreateSuccessResult<T>(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
        };
    }

    public static ApiResponse<object> CreateSuccessResult(string? message = null)
    {
        return new ApiResponse<object>
        {
            Success = true,
            Message = message,
            Data = null,
        };
    }

    public static ApiResponse<object> CreateErrorResult(string message)
    {
        return new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null,
        };
    }
}
