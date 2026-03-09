namespace Application.JWT;

public sealed record JwtOptions(string Secret, string Issuer, string Audience, int ExpiryMinutes)
{
    public const string SectionName = "JWT";
}
