using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Pagination;

namespace Api.Filters;

public sealed class PaginationParametersFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("paginationParameters", out var rawParameter))
        {
            if (rawParameter is PaginationParameters paginationParameters && PaginationParameters.Validate(paginationParameters))
            {
                return;
            }
        }

        context.ActionArguments["paginationParameters"] = new PaginationParameters();
    }
}
