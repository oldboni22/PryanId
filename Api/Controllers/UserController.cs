using System;
using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using Application.Auth;
using Application.Models.User;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Pagination;
using Shared.ResultPattern;


namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> CreateAsync([FromBody] CreateUserModel model, CancellationToken ct)
    {
        var result = await userService.CreateAsync(model);
        
        return result.IsSuccess
            ? Ok()
            : this.ParseFailedResult(result);
    }
    
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReadUserModel>> GetAsync([FromRoute] Guid id, CancellationToken ct)
    {
        var callerId = User.ExtractUserId();
        
        if (callerId == Guid.Empty)
        {
            return BadRequest();
        }

        var result = await userService.GetAsync(id, callerId);
        
        return result.IsSuccess
            ? result.Value
            : this.ParseFailedResult(result);
    }
    
    [HttpPut]
    [Authorize]
    public async Task<ActionResult<ReadUserModel>> UpdateDataAsync([FromBody] UpdateUserDataModel model, CancellationToken ct)
    {
        var userId = User.ExtractUserId();
        
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }

        var result = await userService.UpdateDataAsync(userId, model);
        
        return result.IsSuccess
            ? result.Value
            : this.ParseFailedResult(result);
    }
    
    [Authorize]
    [HttpPut("password")]
    public async Task<ActionResult> UpdatePasswordAsync([FromBody] UpdatePasswordModel model, CancellationToken ct)
    {
        var userId = User.ExtractUserId();
        
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }

        var result = await userService.UpdatePasswordAsync(userId, model);
        
        return result.IsSuccess
            ? Ok()
            : this.ParseFailedResult(result);
    }
    
    [HttpPost("recover-password")]
    [Authorize(ApiKeyRequirement.EmailRecovery)]
    public async Task<ActionResult> RecoverPasswordAsync([FromBody] PasswordRecoveryModel model, CancellationToken ct)
    {
        var result = await userService.RecoverPasswordAsync(model);
        
        return result.IsSuccess
            ? Ok()
            : this.ParseFailedResult(result);
    }
    
    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> DeleteAsync(CancellationToken ct)
    {
        var userId = User.ExtractUserId();
        
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }

        var result = await userService.DeleteAsync(userId);
        
        return result.IsSuccess
            ? Ok()
            : this.ParseFailedResult(result);
    }

    [HttpGet("/{clientId}")]
    [PaginationParametersFilter]
    [Authorize(ClientRoleRequirement.Viewer)]
    public async Task<ActionResult<PagedList<UserClientReadModel>>> GetClientUsersAsync(
        string clientId, [FromQuery] PaginationParameters? paginationParameters)
    {
        var userId = User.ExtractUserId();
        
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        var result = await userService.GetClientUsersAsync(clientId, paginationParameters);
        
        return result.IsSuccess
            ? result.Value
            : this.ParseFailedResult(result);
    }
}
