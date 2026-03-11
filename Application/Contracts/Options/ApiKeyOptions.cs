namespace Application.Contracts.Options;

public record ApiKeyOptions(string Key)
{
    private const string Section = "ApiKeys";

    public const string PasswordRecover = Section + ":PasswordRecover";
}
