namespace Application.Contracts.JWT;

public record struct TokenPair(string AccessToken, string RefreshToken);
