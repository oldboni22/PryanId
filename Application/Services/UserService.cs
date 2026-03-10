using Application.Models.User;
using Domain;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.ResultPattern;

namespace Application.Services;

public interface IUserService
{
    Task<Result> CreateAsync(CreateUserModel createUserModel);
    
    Task<Result<ReadUserModel>> GetAsync(Guid id, Guid callerId);
    
    Task<Result<ReadUserModel>> LoginAsync(string email, string password);
    
    Task<Result<ReadUserModel>> UpdateDataAsync(Guid userId, UpdateUserDataModel updateUserModel);
    
    Task<Result> UpdatePasswordAsync(Guid userId, UpdatePasswordModel updateModel);
    
    Task<Result> RecoverPasswordAsync(Guid userId, string newPassword);
    
    Task<Result> DeleteAsync(Guid userId);
}

public sealed class UserService(UserManager<User> userManager) : IUserService
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
        var result = Result.Success();
        
        var model = await userManager.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new ReadUserModel(
                user.Id,
                user.UserName!, 
                id == callerId ? user.Email! : null) // display email only to the owner 
            )
            .FirstOrDefaultAsync();
        
        if (model is null)
        {
            result.AddError(DomainErrors.UserNotFound);
        }
        
        return Result<ReadUserModel>.FromResult(result, model);
    }

    public async Task<Result<ReadUserModel>> LoginAsync(string email, string password)
    {
        var result = Result.Success();
        
        var user = await userManager.FindByEmailAsync(email);
        
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            result.AddError(DomainErrors.InvalidCredentials);
            return Result<ReadUserModel>.FromResult(result);
        }
        
        var isPasswordCorrect = await userManager.CheckPasswordAsync(user, password);

        if (!isPasswordCorrect)
        {
            await userManager.AccessFailedAsync(user);
            
            result.AddError(DomainErrors.InvalidCredentials);
            return Result<ReadUserModel>.FromResult(result);
        }
        
        await userManager.ResetAccessFailedCountAsync(user);
        
        var model = new ReadUserModel(user.Id, user.UserName!, user.Email!);
        return Result<ReadUserModel>.FromResult(result, model);
    }
    
    public async Task<Result<ReadUserModel>> UpdateDataAsync(Guid userId, UpdateUserDataModel updateUserModel)
    {
        var result = Result.Success();

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            result.AddError(DomainErrors.UserNotFound);
            return Result<ReadUserModel>.FromResult(result);
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
            return Result<ReadUserModel>.FromResult(result, new ReadUserModel(userId, user.UserName!, user.Email!));
        }
        
        EnrichResultFromIdentityResult(result, updateResult);
        return Result<ReadUserModel>.FromResult(result);
    }

    public async Task<Result> UpdatePasswordAsync(Guid userId, UpdatePasswordModel updateModel)
    {
        var result = Result.Success();
        
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            result.AddError(DomainErrors.UserNotFound);
            return result;
        }
        
        var identityResult = await userManager.ChangePasswordAsync(user, updateModel.OldPassword, updateModel.NewPassword);

        if (!identityResult.Succeeded)
        {
            EnrichResultFromIdentityResult(result, identityResult);
        }
        
        return result;
    }
    
    public async Task<Result> RecoverPasswordAsync(Guid userId, string newPassword)
    {
        var result = Result.Success();
        
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            result.AddError(DomainErrors.UserNotFound);
            return result;
        }
        
        await userManager.RemovePasswordAsync(user);
        var identityResult = await userManager.AddPasswordAsync(user, newPassword);

        if (!identityResult.Succeeded)
        {
            EnrichResultFromIdentityResult(result, identityResult);
        }
        
        return result;
    }
    
    public async Task<Result> DeleteAsync(Guid userId)
    {
        var result = Result.Success();
        
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            result.AddError(DomainErrors.UserNotFound);
            return result;
        }
        
        var deleteResult = await userManager.DeleteAsync(user);

        if (!deleteResult.Succeeded)
        {
            EnrichResultFromIdentityResult(result, deleteResult);
        }

        return result;
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
