namespace Application.Models.Client;

public sealed record UpdateClientModel(
    string? ClientName = null,
    List<string>? AllowedScopes = null,
    List<string>? AllowedGrantTypes = null,
    bool AllowOfflineAccess = false);
