namespace Application.Models.User;

public sealed record PasswordRecoveryModel(string Email, string NewPassword);
