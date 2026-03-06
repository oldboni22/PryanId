namespace Domain.ValueObjects;

public sealed record DisplayName
{
    public const int MaxLength = 15;
    
    private const int MinLength = 3;
    
    public string Value { get; private set; }

    private DisplayName(string value) => Value = value;
    
    public static DisplayName? Create(string value)
    {
        return Validate(value) 
            ? new(value)
            : null;
    }
    
    private static bool Validate(string value) => value.Trim().Length is >= MinLength and <= MaxLength;
    
    public override string ToString() => Value;

    public static DisplayName FromDatabase(string value) => new(value);
}
