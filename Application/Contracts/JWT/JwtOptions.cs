namespace Application.Contracts.JWT;

public sealed record JwtOptions(string Secret, string Issuer, string Audience, int AccessExpiryMinutes, int RefreshExpiryDays)
{
    public const string SectionName = "JWT";
}
