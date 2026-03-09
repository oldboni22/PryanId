namespace Application.Models.User;

public sealed record UpdatePasswordModel(string OldPassword, string NewPassword);
