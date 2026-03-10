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
    [HttpPost("login")]
    public async Task<ActionResult<TokenPair>> LoginAsync([FromBody] LoginUserModel model, CancellationToken ct)
    {
        var loginResult = await userService.LoginAsync(model);
        
        return loginResult.IsSuccess
            ? loginResult.Value
            : this.ParseFailedResult(loginResult);
    }
}
