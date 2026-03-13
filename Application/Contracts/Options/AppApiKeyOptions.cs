namespace Application.Contracts.Options;

public record AppApiKeyOptions(string Key)
{
    private const string Section = "ApiKeys";

    public const string PasswordRecover = Section + ":PasswordRecover";
}
