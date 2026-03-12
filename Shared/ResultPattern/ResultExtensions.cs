using System.Linq;
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
                ErrorType.Unauthorized => controller.Unauthorized(result.Errors), 
                ErrorType.Forbidden => controller.Forbid(),                       
                ErrorType.Locked => controller.StatusCode(423, result.Errors),   
                _ => controller.StatusCode(500, result.Errors)
            };
        }
    }
}
