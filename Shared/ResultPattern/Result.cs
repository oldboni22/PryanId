namespace Shared.ResultPattern;

public class Result
{
    protected internal Result(List<Error> errors) => _errors = errors;

    protected internal Result() => _errors = [];
    
    protected readonly List<Error> _errors;
    
    public IEnumerable<Error> Errors => _errors;
    
    public bool IsSuccess => _errors.Count == 0;

    public void AddError(Error error) => _errors.Add(error);
    
    public static Result Success() => new();
    
    public static Result FromError(Error error) => new Result([ error ]);
}

public class Result<TValue> : Result
{
    protected internal Result(TValue value)
    {
        Value = value;
    }

    private Result(TValue? value, IEnumerable<Error> errors)
    {
        Value = value;
        _errors.AddRange(errors);
    }
    
    public TValue Value => IsSuccess 
        ? field! 
        : throw new InvalidOperationException("The value of a failure result can't be accessed.");

    public static Result<TValue> FromResult(Result result, TValue? value = default) =>
        new Result<TValue>(value, result.Errors);
    
    public static Result<TValue> FromResult<T2>(Result<T2> result, TValue? value = default) =>
        new Result<TValue>(value, result.Errors);
    
    public static Result<TValue> FromError(Error error) => new Result<TValue>(default, [ error ]);
    
    public static Result<TValue> Success(TValue value) => new Result<TValue>(value);
}
