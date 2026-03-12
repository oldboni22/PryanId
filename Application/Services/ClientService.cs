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
using Shared;
using Shared.ResultPattern;

namespace Application.Services;

public interface IClientService
{
    Task<Result<ClientSecretModel>> CreateClientAsync(Guid userId, CreateClientModel model);
    
    Task<Result> DeleteClientAsync(string clientId);
    
    Task<Result<ClientSecretModel>> RotateSecretAsync(string clientId);
    
    Task<Result> UpdateClientAsync(string clientId, UpdateClientModel model);
}

public sealed class ClientService(
    IUserDbContext userContext, 
    ConfigurationDbContext clientContext,
    TimeProvider timeProvider) : IClientService
{
    public async Task<Result<ClientSecretModel>> CreateClientAsync(Guid userId, CreateClientModel model)
    {
        if (await clientContext.Clients.AnyAsync(c => c.ClientId == model.ClientId))
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

        await userContext.UserClients.AddAsync(new UserClient
        {
            ClientId =  client.ClientId,
            UserId = userId,
            Role = UserClientRole.Admin
        });
        await userContext.SaveChangesAsync();
        
        await clientContext.AddAsync(entity);
        await clientContext.SaveChangesAsync();

        return Result<ClientSecretModel>.Success(new ClientSecretModel(model.ClientId, secret));
    }
    
    public async Task<Result> DeleteClientAsync(string clientId)
    {
        var client = await clientContext.Clients.FirstOrDefaultAsync(x => x.ClientId == clientId);
        
        if (client is null)
        {
            return Result.FromError(DomainErrors.ClientNotFound);
        }

        clientContext.Clients.Remove(client);
        await clientContext.SaveChangesAsync();
        
        return Result.Success();
    }
    
    public async Task<Result<ClientSecretModel>> RotateSecretAsync(string clientId)
    {
        var client = await clientContext.Clients
            .Include(x => x.ClientSecrets)
            .FirstOrDefaultAsync(x => x.ClientId == clientId);

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

        await clientContext.SaveChangesAsync();
        return Result<ClientSecretModel>.Success(new ClientSecretModel(clientId, newSecret));
    }
    
    public async Task<Result> UpdateClientAsync(string clientId, UpdateClientModel model)
    {
        var client = await clientContext.Clients
            .Include(x => x.AllowedScopes)
            .Include(x => x.AllowedGrantTypes)
            .FirstOrDefaultAsync(x => x.ClientId == clientId);

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

        await clientContext.SaveChangesAsync();
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
}
