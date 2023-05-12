using System.Runtime.Serialization;

namespace Core.CrossCuttingConcerns.Exceptions.Types;

public class NotFoundException : Exception
{
    public NotFoundException() { }

    protected NotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }

    public NotFoundException(string? message)
        : base(message) { }

    public NotFoundException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
