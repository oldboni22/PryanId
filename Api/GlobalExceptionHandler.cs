using Microsoft.AspNetCore.Diagnostics;

namespace Api;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.UnhandledException(exception);
        
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new { StatusCode = 500, Detail = "Internal server error." }, cancellationToken);

        return true;
    }
}
