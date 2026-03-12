using Application.Auth;
using Application.Models.Client;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
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
        
        var result = await clientService.CreateClientAsync(userId, model, ct);

        return result.IsSuccess 
            ? result.Value 
            : this.ParseFailedResult(result);
    }
    
    [HttpPatch("{clientId}")]
    [Authorize(ProjectRoleRequirement.Editor)]
    public async Task<ActionResult> UpdateAsync(string clientId, [FromBody] UpdateClientModel model)
    {
        var result = await clientService.UpdateClientAsync(clientId, model);

        return result.IsSuccess 
            ? NoContent() 
            : this.ParseFailedResult(result);
    }
    
    [HttpPost("{clientId}/rotate-secret")]
    [Authorize(ProjectRoleRequirement.Admin)]
    public async Task<ActionResult<ClientSecretModel>> RotateSecret(string clientId)
    {
        var result = await clientService.RotateSecretAsync(clientId);

        return result.IsSuccess 
            ? Ok(result.Value) 
            : this.ParseFailedResult(result);
    }
    
    [HttpDelete("{clientId}")]
    [Authorize(ProjectRoleRequirement.Owner)]
    public async Task<IActionResult> Delete(string clientId)
    {
        var result = await clientService.DeleteClientAsync(clientId);

        return result.IsSuccess 
            ? NoContent() 
            : this.ParseFailedResult(result);
    }
}
