namespace NArchitecture.Core.Mediator;

/// <summary>Represents a void response. Used by <see cref="IRequest"/>.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;

    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(Value);

    public override string ToString() => "()";
}
