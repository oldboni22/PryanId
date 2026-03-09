using Shared.ResultPattern;

namespace Domain;

public static class DomainErrors
{
    public static Error UserNotFound =>
        new Error("User.NotFound", "The used was not found.", ErrorType.NotFound);
    
    public static Error InvalidCredentials =>
        new Error("Credentials.Invalid", "Invalid user credentials.", ErrorType.Unauthorized);
    
    #region UserName
    
    public static Error UserNameShort =>
        new Error("User.Name.Short", "The user name is too short.", ErrorType.Validation);
    
    public static Error UserNameLong =>
        new Error("User.Name.Long", "The user name is too long.", ErrorType.Validation);

    public static Error DuplicateUserName => 
        new Error("User.Name.Duplicate","The user name is already in use.", ErrorType.Validation);
    
    public static Error IncorrectUserName =>
        new Error("User.Name.Incorrect", "The user name is incorrect.", ErrorType.Validation);
    
    #endregion
    
    #region Email

    public static Error DuplicateUserEmail => 
        new Error("User.Email.Duplicate","The user email is already in use.", ErrorType.Validation);
    
    public static Error IncorrectUserEmail => 
        new Error("User.Email.Incorrect","The user email is incorrect.", ErrorType.Validation);
    
    #endregion

    #region Password

    public static Error PasswordShort => 
        new Error("Password.Short","The password is too short.", ErrorType.Validation);

    public static Error PasswordNoNonAlphanumeric => 
        new Error("Password.NoNonAlphanumeric","The password requires a non alphanumeric character.", ErrorType.Validation);
    
    public static Error PasswordNoDigit => 
        new Error("Password.NoDigit","The password requires a digit.", ErrorType.Validation);
    
    public static Error PasswordIncorrect => 
        new Error("Password.Incorrect","The password is incorrect.", ErrorType.Unauthorized);
    
    #endregion
}
