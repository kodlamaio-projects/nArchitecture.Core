using FluentValidation;
using NArchitecture.Core.Application.Pipelines.Validation;
using NArchitecture.Core.Mediator;
using ValidationException = NArchitecture.Core.CrossCuttingConcerns.Exception.Types.ValidationException;

namespace Core.Application.Tests.Validation;

public class RequestValidationBehaviorTests
{
    private sealed record CreateThing(string Name, int Quantity) : IRequest<string>;

    private static InlineValidator<CreateThing> validator()
    {
        InlineValidator<CreateThing> v = new();
        v.RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
        v.RuleFor(x => x.Quantity).GreaterThan(0);
        return v;
    }

    [Fact]
    public async Task Valid_request_passes_through()
    {
        RequestValidationBehavior<CreateThing, string> behavior = new([validator()]);

        string response = await behavior.Handle(
            new CreateThing("abc", 1),
            _ => Task.FromResult("ok"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("ok", response);
    }

    [Fact]
    public async Task Invalid_request_throws_grouped_by_property_and_skips_handler()
    {
        RequestValidationBehavior<CreateThing, string> behavior = new([validator()]);
        bool handlerCalled = false;

        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new CreateThing("", 0),
                _ =>
                {
                    handlerCalled = true;
                    return Task.FromResult("ok");
                },
                TestContext.Current.CancellationToken
            )
        );

        Assert.False(handlerCalled);
        Assert.Equal(["Name", "Quantity"], ex.Errors.Select(e => e.Property).Order());
        Assert.Equal(2, ex.Errors.Single(e => e.Property == "Name").Errors!.Count());
    }

    [Fact]
    public async Task No_validators_means_no_validation()
    {
        RequestValidationBehavior<CreateThing, string> behavior = new([]);

        string response = await behavior.Handle(
            new CreateThing("", 0),
            _ => Task.FromResult("ok"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("ok", response);
    }
}
