using Shared.ResultPattern;

namespace Application;

public static class AuthErrors
{
    public static Error InvalidRefreshToken => 
        new Error("Auth.RefreshToken.Invalid", "Invalid refresh token.", ErrorType.Validation);
    
    public static Error CompromisedRefreshToken => 
        new Error("Auth.RefreshToken.Compromised", "Compromised refresh token.", ErrorType.Validation);
    
    public static Error ExpiredRefreshToken => 
        new Error("Auth.RefreshToken.Expired", "Expired refresh token.", ErrorType.Validation);
}
