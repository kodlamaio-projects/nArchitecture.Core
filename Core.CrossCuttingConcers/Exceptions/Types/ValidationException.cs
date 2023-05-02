namespace Core.CrossCuttingConcerns.Exceptions.Types;

public class ValidationException : Exception
{
    public IEnumerable<ValidationExceptionModel> Errors { get; }

    public ValidationException(IEnumerable<ValidationExceptionModel> errors)
        : base(BuildErrorMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildErrorMessage(IEnumerable<ValidationExceptionModel> errors)
    {
        IEnumerable<string> arr = errors.Select(
            x => $"{Environment.NewLine} -- {x.Property}: {string.Join(Environment.NewLine, values: x.Errors ?? Array.Empty<string>())}"
        );
        return $"Validation failed: {string.Join(string.Empty, arr)}";
    }
}

public class ValidationExceptionModel
{
    public string? Property { get; set; }
    public IEnumerable<string>? Errors { get; set; }
}
