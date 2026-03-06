using Shared.ResultPattern;

namespace Domain;

public static class DomainErrors
{
    public static Error UserNameShort =>
        new Error("User.Name.Short", "The user name is too short.", ErrorType.Validation);
    
    public static Error UserNameLong =>
        new Error("User.Name.Long", "The user name is too long.", ErrorType.Validation);

    public static Error DuplicateUserName => 
        new Error("User.Name.Duplicate","The user name is already in use.", ErrorType.Validation);
    
    public static Error DuplicateUserEmail => 
        new Error("User.Email.Duplicate","The user email is already in use.", ErrorType.Validation);
    
    public static Error IncorrectUserEmail => 
        new Error("User.Email.Incorrect","The user email is incorrect.", ErrorType.Validation);
}
