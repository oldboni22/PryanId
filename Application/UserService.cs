using Application.Models.User;
using Domain;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Shared.ResultPattern;

namespace Application;

public sealed class UserService(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
{
    public async Task<Result> CreateUserAsync(CreateUserModel createUserModel)
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

        EnrichResultFromIdentityResult(ref result, createResult);
        
        return result;
    }

    private static void EnrichResultFromIdentityResult(ref Result result, IdentityResult identityResult)
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
                
                case "DuplicateEmail":
                    result.AddError(DomainErrors.DuplicateUserEmail);
                    break;
                case "InvalidEmail":
                    result.AddError(DomainErrors.IncorrectUserEmail);
                    break;
            }
        }
    }
}
