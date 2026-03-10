using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Contracts.JWT;
using Application.Models.User;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.ResultPattern;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<TokenPair>> LoginAsync([FromBody] LoginUserModel model, CancellationToken ct)
    {
        var loginResult = await authService.LoginAsync(model);
        
        return loginResult.IsSuccess
            ? loginResult.Value
            : this.ParseFailedResult(loginResult);
    }

    [HttpGet("relogin")]
    public async Task<ActionResult<TokenPair>> ReLoginAsync([FromBody] string oldTokenLiteral, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(oldTokenLiteral, ct);
        
        return result.IsSuccess
            ? result.Value
            : this.ParseFailedResult(result);
    }
    
    [Authorize]
    [HttpPost("terminate")]
    public async Task<ActionResult> TerminateAsync(CancellationToken ct)
    {
        var userId = User.ExtractUserId();
        
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        await authService.InvalidateAllSessions(userId, ct);
        return Ok();
    }
}
