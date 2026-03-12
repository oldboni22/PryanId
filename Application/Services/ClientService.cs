using System.Security.Cryptography;
using Application.Contracts.Db;
using Application.Models.Client;
using Domain;
using Domain.Entities;
using Domain.Enums;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.ResultPattern;

namespace Application.Services;

public interface IClientService
{
    Task<Result<ClientSecretModel>> CreateClientAsync(Guid userId, CreateClientModel model, CancellationToken ct = default);
    
    Task<Result> DeleteClientAsync(string clientId, CancellationToken ct = default);
    
    Task<Result<ClientSecretModel>> RotateSecretAsync(string clientId, CancellationToken ct = default);
    
    Task<Result> UpdateClientAsync(string clientId, UpdateClientModel model, CancellationToken ct = default);
}

public sealed class ClientService(
    IUserDbContext userContext, 
    ConfigurationDbContext clientContext,
    TimeProvider timeProvider,
    ILogger<ClientService> logger) : IClientService
{
    #region Client management
    
    public async Task<Result<ClientSecretModel>> CreateClientAsync(
        Guid userId, CreateClientModel model, CancellationToken ct = default)
    {
        if (await clientContext.Clients.AnyAsync(c => c.ClientId == model.ClientId, cancellationToken: ct))
        {
            return Result<ClientSecretModel>.FromError(DomainErrors.ClientExists);
        }
        
        var secret = GenerateSecret();

        var client = new Client
        {
            ClientId = model.ClientId,
            ClientName = model.ClientName,
            AllowedGrantTypes = GetGrantTypes(model.GrantType),
            ClientSecrets = { new Secret(secret.Sha256()) }
        };

        var entity = client.ToEntity();
        var timeNow = timeProvider.UtcNow;
        
        foreach (var clientSecret in entity.ClientSecrets)
        {
            clientSecret.Created = timeNow;
        }

        await clientContext.AddAsync(entity, ct);
        await clientContext.SaveChangesAsync(ct);

        try
        {
            await userContext.UserClients.AddAsync(new UserClient
            {
                ClientId = client.ClientId,
                UserId = userId,
                Role = UserClientRole.Owner
            }, ct);

            await userContext.SaveChangesAsync(ct);
        }
        catch(Exception e)
        {
            logger.ClientCreationFailed(model.ClientId, userId, e);
            
            clientContext.Remove(entity);
            await clientContext.SaveChangesAsync(ct);
            
            return  Result<ClientSecretModel>.FromError(Error.Unknown);
        }
        
        return Result<ClientSecretModel>.Success(new ClientSecretModel(model.ClientId, secret));
    }
    
    public async Task<Result> DeleteClientAsync(string clientId, CancellationToken ct = default)
    {
        var client = await clientContext.Clients.FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken: ct);
        
        if (client is null)
        {
            return Result.FromError(DomainErrors.ClientNotFound);
        }

        clientContext.Clients.Remove(client);
        await clientContext.SaveChangesAsync(ct);
        
        return Result.Success();
    }
    
    public async Task<Result<ClientSecretModel>> RotateSecretAsync(string clientId, CancellationToken ct = default)
    {
        var client = await clientContext.Clients
            .Include(x => x.ClientSecrets)
            .FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken: ct);

        if (client is null)
        {
            return Result<ClientSecretModel>.FromError(DomainErrors.ClientNotFound);
        }
        
        var newSecret = GenerateSecret();
        
        client.ClientSecrets.Clear();
        
        client.ClientSecrets.Add(new Duende.IdentityServer.EntityFramework.Entities.ClientSecret
        {
            Value = newSecret.Sha256(),
            Type = "SharedSecret",
            Created = timeProvider.UtcNow
        });

        await clientContext.SaveChangesAsync(ct);
        return Result<ClientSecretModel>.Success(new ClientSecretModel(clientId, newSecret));
    }
    
    public async Task<Result> UpdateClientAsync(string clientId, UpdateClientModel model, CancellationToken ct = default)
    {
        var client = await clientContext.Clients
            .Include(x => x.AllowedScopes)
            .Include(x => x.AllowedGrantTypes)
            .FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken: ct);

        if (client is null)
        {
            return Result.FromError(DomainErrors.ClientNotFound);
        }
        
        client.AllowOfflineAccess = model.AllowOfflineAccess;
        
        if (model.ClientName is not null)
        {
            client.ClientName = model.ClientName;
        }
        
        if (model.AllowedScopes is not null)
        {
            client.AllowedScopes.Clear();
            client.AllowedScopes.AddRange(model.AllowedScopes.Select(scope => 
                new Duende.IdentityServer.EntityFramework.Entities.ClientScope 
                { 
                    Scope = scope, 
                    ClientId = client.Id
                }));
        }
        
        if (model.AllowedGrantTypes is not null)
        {
            client.AllowedGrantTypes.Clear();
            client.AllowedGrantTypes.AddRange(model.AllowedGrantTypes.Select(grantType => 
                new Duende.IdentityServer.EntityFramework.Entities.ClientGrantType 
                { 
                    GrantType = grantType, 
                    ClientId = client.Id 
                }));
        }

        await clientContext.SaveChangesAsync(ct);
        return Result.Success();
    }
    
    private static ICollection<string> GetGrantTypes(string type) => type.ToLower() switch
    {
        "m2m" => GrantTypes.ClientCredentials,
        "password" => GrantTypes.ResourceOwnerPassword,
        "interactive" => GrantTypes.Code,
        _ => GrantTypes.Code
    };
    
    private static string GenerateSecret() => Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    
    #endregion

    #region Role management

    public async Task<Result> ChangeUserRole(
        string clientId, Guid targetId, Guid promoterId, UserClientRole targetRole, CancellationToken ct = default)
    {
        if (targetId == promoterId)
        {
            return Result.FromError(DomainErrors.SelfPromote);
        }

        var promoterRelation = await GetUserRole(clientId, promoterId, ct);

        if (promoterRelation is null)
        {
            return Result.FromError(DomainErrors.CantPromote);
        }
        
        if(promoterRelation.Role < UserClientRole.Admin
           || (targetRole == UserClientRole.Admin && promoterRelation.Role != UserClientRole.Owner) /* Check for admin promotion*/
           || (targetRole == UserClientRole.Owner && promoterRelation.Role != UserClientRole.Owner) /* Check for owner promotion*/
           )
        {
            return Result.FromError(DomainErrors.CantPromote);
        }
        
        var existingRelation = await GetUserRole(clientId, targetId, ct);

        if (existingRelation is not null)
        {
            if (existingRelation.Role == UserClientRole.Owner ^ /* No one can change owner's role*/
                (existingRelation.Role == UserClientRole.Admin && promoterRelation.Role != UserClientRole.Owner)
                /* Only owner can change admin's role*/
               )
            {
                return Result.FromError(DomainErrors.CantPromote);
            }
        }
        
        if (promoterRelation.Role == UserClientRole.Owner && targetRole == UserClientRole.Owner)
        {
            promoterRelation.Role = UserClientRole.Admin;
        }
        
        if (existingRelation is null)
        {
            userContext.UserClients.Add(new UserClient
            {
                ClientId = clientId,
                UserId = targetId,
                Role = targetRole
            });
        }
        else
        {
            existingRelation.Role = targetRole;    
        }
        
        await userContext.SaveChangesAsync(ct);
        return Result.Success();
    }
    
    private async Task<UserClient?> GetUserRole(string clientId, Guid userId, CancellationToken ct)
    {
        return await userContext.UserClients
            .FirstOrDefaultAsync(uc => uc.ClientId == clientId && uc.UserId == userId, ct);
    }

    #endregion
}
