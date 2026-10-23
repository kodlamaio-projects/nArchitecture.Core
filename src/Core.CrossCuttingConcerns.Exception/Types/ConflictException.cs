using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

public class ConflictException : System.Exception
{
    public ConflictException() { }

    public ConflictException(string? message)
        : base(message) { }

    public ConflictException(string? message, System.Exception? innerException)
        : base(message, innerException) { }
}
