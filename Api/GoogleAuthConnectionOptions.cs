namespace Api;

public sealed record GoogleAuthConnectionOptions(string ClientId, string ClientSecret)
{
    public const string SectionName = "GoogleAuth";
}
