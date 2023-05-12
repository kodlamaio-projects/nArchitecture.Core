using System.Runtime.Serialization;

namespace Core.CrossCuttingConcerns.Exceptions.Types;

public class AuthorizationException : Exception
{
    public AuthorizationException() { }

    protected AuthorizationException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }

    public AuthorizationException(string? message)
        : base(message) { }

    public AuthorizationException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
