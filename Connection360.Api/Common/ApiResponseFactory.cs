using Connection360.Api.Models;

namespace Connection360.Api.Common
{
    public static class ApiResponseFactory
    {
        public static ApiResponse<T> Success<T>(T data, String message, Int32 status, String path, MetaResponse? meta = null)
            => new()
            {
                Status = status,
                Message = message,
                DataResponse = data,
                Meta = meta,
                Path = path
            };

        public static ApiResponse<Object> Fail(String error, Int32 status, String path, String? message = null)
            => new()
            {
                Status = status,
                Error = error,
                Message = message ?? "Ocurrió un error al procesar la solicitud",
                Path = path
            };
    }
}
