using Application.Contracts.Db;
using Application.Models.User;
using Domain;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Db;
using Shared.Pagination;
using Shared.ResultPattern;

namespace Application.Services;

public interface IUserService
{
    Task<Result> CreateAsync(CreateUserModel createUserModel);
    
    Task<Result<ReadUserModel>> GetAsync(Guid id, Guid callerId);
    
    Task<Result<ReadUserModel>> UpdateDataAsync(Guid userId, UpdateUserDataModel updateUserModel);
    
    Task<Result> UpdatePasswordAsync(Guid userId, UpdatePasswordModel updateModel);
    
    Task<Result> RecoverPasswordAsync(PasswordRecoveryModel model);
    
    Task<Result> DeleteAsync(Guid userId);

    Task<Result<PagedList<UserClientReadModel>>> GetClientUsersAsync(
        string clientId, PaginationParameters? paginationParameters = null, CancellationToken ct = default);
}

public sealed class UserService(UserManager<User> userManager, IUserDbContext dbContext) : IUserService
{
    public async Task<Result> CreateAsync(CreateUserModel createUserModel)
    {
        var result = Result.Success();
        
        var user = new User
        {
            UserName = createUserModel.Name,
            Email = createUserModel.Mail,
        };
        
        var createResult = await userManager.CreateAsync(user, createUserModel.Password);
        
        if (createResult.Succeeded)
        {
            return Result.Success();
        }

        EnrichResultFromIdentityResult(result, createResult);
        
        return result;
    }

    public async Task<Result<ReadUserModel>> GetAsync(Guid id, Guid callerId)
    {
        var model = await userManager.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new ReadUserModel(
                user.Id,
                user.UserName!, 
                id == callerId ? user.Email! : null) // display email only to the owner 
            )
            .FirstOrDefaultAsync();
        
        return model is null
            ? Result<ReadUserModel>.FromError(DomainErrors.UserNotFound)
            : Result<ReadUserModel>.Success(model);
    }
    
    public async Task<Result<ReadUserModel>> UpdateDataAsync(Guid userId, UpdateUserDataModel updateUserModel)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<ReadUserModel>.FromError(DomainErrors.UserNotFound);
        }

        if(updateUserModel.NewEmail is not  null)
        {
            user.Email = updateUserModel.NewEmail ?? user.Email;
            user.EmailConfirmed = false;
        }
        
        user.UserName = updateUserModel.NewName  ?? user.UserName;
        
        var updateResult = await userManager.UpdateAsync(user);

        if (updateResult.Succeeded)
        {
            return Result<ReadUserModel>.Success(new ReadUserModel(userId, user.UserName!, user.Email!));
        }
        
        var result = Result.Success();
        EnrichResultFromIdentityResult(result, updateResult);
        return Result<ReadUserModel>.FromResult(result);
    }

    public async Task<Result> UpdatePasswordAsync(Guid userId, UpdatePasswordModel updateModel)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.FromError(DomainErrors.UserNotFound);
        }
        
        var identityResult = await userManager.ChangePasswordAsync(user, updateModel.OldPassword, updateModel.NewPassword);

        var result = Result.Success();
        
        if (!identityResult.Succeeded)
        {
            EnrichResultFromIdentityResult(result, identityResult);
        }
        
        return result;
    }
    
    public async Task<Result> RecoverPasswordAsync(PasswordRecoveryModel model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return Result.FromError(DomainErrors.UserNotFound);
        }
        
        await userManager.RemovePasswordAsync(user);
        var identityResult = await userManager.AddPasswordAsync(user, model.NewPassword);

        var result = Result.Success();
        
        if (!identityResult.Succeeded)
        {
            EnrichResultFromIdentityResult(result, identityResult);
        }
        
        return result;
    }
    
    public async Task<Result> DeleteAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.FromError(DomainErrors.UserNotFound);
        }
        
        var deleteResult = await userManager.DeleteAsync(user);

        var result = Result.Success();
        
        if (!deleteResult.Succeeded)
        {
            EnrichResultFromIdentityResult(result, deleteResult);
        }

        return result;
    }

    public async Task<Result<PagedList<UserClientReadModel>>> GetClientUsersAsync(
        string clientId, PaginationParameters? paginationParameters = null, CancellationToken ct = default)
    {
        paginationParameters ??= new PaginationParameters();

        var relationsQuery = dbContext.UserClients
            .AsNoTracking()
            .Where(uc => uc.ClientId == clientId);
        
        var totalCount = await relationsQuery.CountAsync(ct);

        if (totalCount == 0)
        {
            return Result<PagedList<UserClientReadModel>>.FromError(DomainErrors.ClientNotFound);
        }
        
        var models = await relationsQuery
            .OrderByDescending(uc => uc.Role)
            .ThenBy(uc => uc.User.UserName)
            .Select(uc => new UserClientReadModel(
                uc.User.Id, 
                uc.User.UserName!, 
                uc.Role)
            )
            .Page(paginationParameters)
            .ToListAsync(cancellationToken: ct);
        
        return Result<PagedList<UserClientReadModel>>.Success(
            PagedList<UserClientReadModel>.Create(models, paginationParameters, totalCount));
    }

    private static void EnrichResultFromIdentityResult(Result result, IdentityResult identityResult)
    {
        foreach (var err in identityResult.Errors)
        {
            switch (err.Code)
            {
                case "DuplicateUserName":
                    result.AddError(DomainErrors.DuplicateUserName);
                    break;
                case "UserNameTooShort":
                    result.AddError(DomainErrors.UserNameShort);
                    break;
                case "UserNameTooLong":
                    result.AddError(DomainErrors.UserNameLong);
                    break;
                case "InvalidUserName":
                    result.AddError(DomainErrors.IncorrectUserName);
                    break;
                
                case "DuplicateEmail":
                    result.AddError(DomainErrors.DuplicateUserEmail);
                    break;
                case "InvalidEmail":
                    result.AddError(DomainErrors.IncorrectUserEmail);
                    break; 
                
                case "PasswordTooShort":
                    result.AddError(DomainErrors.PasswordShort);
                    break;
                case "PasswordRequiresNonAlphanumeric":
                    result.AddError(DomainErrors.PasswordNoNonAlphanumeric);
                    break;
                case "PasswordRequiresDigit":
                    result.AddError(DomainErrors.PasswordNoDigit);
                    break;
                case "PasswordMismatch":
                    result.AddError(DomainErrors.PasswordIncorrect); 
                    break;

                default: result.AddError(Error.Unknown);
                    break;
            }
        }
    }
}
