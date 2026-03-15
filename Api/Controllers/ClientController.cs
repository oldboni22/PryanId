using System.Threading;
using System.Threading.Tasks;
using Api.Filters;
using Application.Auth;
using Application.Models.Client;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Pagination;
using Shared.ResultPattern;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ClientsController(IClientService clientService) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ClientSecretModel>> CreateAsync([FromBody] CreateClientModel model, CancellationToken ct)
    {
        var userId = User.ExtractUserId();

        if (userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        var result = await clientService.CreateClientAsync(userId, model, ct);

        return result.IsSuccess 
            ? result.Value 
            : this.ParseFailedResult(result);
    }
    
    [HttpPatch("{clientId}")]
    [Authorize(ClientRoleRequirement.Editor)]
    public async Task<ActionResult> UpdateAsync(string clientId, [FromBody] UpdateClientModel model)
    {
        var result = await clientService.UpdateClientAsync(clientId, model);

        return result.IsSuccess 
            ? NoContent() 
            : this.ParseFailedResult(result);
    }
    
    [HttpPost("{clientId}/rotate-secret")]
    [Authorize(ClientRoleRequirement.Admin)]
    public async Task<ActionResult<ClientSecretModel>> RotateSecret(string clientId)
    {
        var result = await clientService.RotateSecretAsync(clientId);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : this.ParseFailedResult(result);
    }
    
    [HttpDelete("{clientId}")]
    [Authorize(ClientRoleRequirement.Owner)]
    public async Task<ActionResult> Delete(string clientId)
    {
        var result = await clientService.DeleteClientAsync(clientId);

        return result.IsSuccess 
            ? NoContent() 
            : this.ParseFailedResult(result);
    }

    [HttpPost("{clientId}/change-role")]
    [Authorize(ClientRoleRequirement.Admin)]
    public async Task<ActionResult> ChangeUserRoleAsync([FromRoute] string clientId, [FromBody] ChangeUserClientRoleModel model)
    {
        var promoterId = User.ExtractUserId();

        if (promoterId == Guid.Empty)
        {
            return BadRequest();
        }
        
        var result = await clientService.ChangeUserRole(clientId, promoterId, model);
        
        return result.IsSuccess 
            ? NoContent() 
            : this.ParseFailedResult(result);
    }

    [Authorize]
    [PaginationParametersFilter]
    [HttpGet("/{userId:guid}")]
    public async Task<ActionResult<PagedList<ClientUserReadModel>>> GetUserClientsAsync(
        [FromQuery] PaginationParameters? paginationParameters = null, CancellationToken ct = default)
    {
        var userId = User.ExtractUserId();

        if (userId == Guid.Empty)
        {
            return BadRequest();
        }

        var result = await clientService.GetUserClientsAsync(userId, paginationParameters, ct);
        
        return result.IsSuccess
            ? Ok(result.Value)
            : this.ParseFailedResult(result);
    }
}
