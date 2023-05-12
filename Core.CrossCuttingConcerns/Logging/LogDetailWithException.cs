namespace Core.CrossCuttingConcerns.Logging;

public class LogDetailWithException : LogDetail
{
    public string ExceptionMessage { get; set; }

    public LogDetailWithException()
    {
        ExceptionMessage = string.Empty;
    }

    public LogDetailWithException(string fullName, string methodName, string user, List<LogParameter> parameters, string exceptionMessage)
        : base(fullName, methodName, user, parameters)
    {
        ExceptionMessage = exceptionMessage;
    }
}
