namespace Shared.ResultPattern;

public class Result
{
    protected readonly List<Error> _errors = [];
    
    public IEnumerable<Error> Errors => _errors;
    
    public bool IsSuccess => _errors.Count == 0;

    public void AddError(Error error) => _errors.Add(error);
    
    public static Result Success() => new();

    public static Result<TValue> Success<TValue>(TValue value) => new(value);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value)
    {
        _value = value;
    }

    private Result(TValue? value, IEnumerable<Error> errors)
    {
        _value = value;
        _errors.AddRange(errors);
    }
    
    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("The value of a failure result can't be accessed.");

    public static Result<TValue> FromResult(Result failedResult, TValue? value = default) =>
        new Result<TValue>(value, failedResult.Errors);
    
    public static Result<TValue> FromResult<T2>(Result<T2> failedResult, TValue? value = default) =>
        new Result<TValue>(value, failedResult.Errors);
    
    public static implicit operator Result<TValue>(TValue? value) => Success(value)!;
}
