namespace Application.Models.User;

public sealed record UpdateUserDataModel(string? NewName = null, string? NewEmail = null);
