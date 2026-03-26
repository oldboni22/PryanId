namespace Application.Models.User;

public sealed record PasswordRecoveryModel(string Token, string Email, string NewPassword);
