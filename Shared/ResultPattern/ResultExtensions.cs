using Microsoft.AspNetCore.Mvc;

namespace Shared.ResultPattern;

public static class ResultExtensions
{
    extension(ControllerBase controller)
    {
        public ActionResult ParseFailedResult(Result result)
        {
            var error = result.Errors.First();
            
            return error.Type switch
            {
                ErrorType.Validation => controller.BadRequest(result.Errors),
                ErrorType.NotFound => controller.NotFound(result.Errors),
                ErrorType.Unauthorized => controller.Unauthorized(result.Errors), // Вернет 401
                ErrorType.Forbidden => controller.Forbid(),                       // Вернет 403
                ErrorType.Locked => controller.StatusCode(423, result.Errors),    // Вернет 423
                _ => controller.StatusCode(500, result.Errors)
            };
        }
    }
}
