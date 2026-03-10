using Application.Contracts.JWT;
using Application.Models.User;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.ResultPattern;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService, IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TokenPair>> LoginAsync([FromBody] LoginUserModel model, CancellationToken ct)
    {
        var loginResult = await userService.LoginAsync(model);

        if (!loginResult.IsSuccess)
        {
            return this.ParseFailedResult(loginResult);
        }
        
        return await authService.IssueAsync(loginResult.Value.Id, loginResult.Value.Email!, ct);
    }
}
