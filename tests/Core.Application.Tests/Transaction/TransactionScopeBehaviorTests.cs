using System.Transactions;
using NArchitecture.Core.Application.Pipelines.Transaction;
using NArchitecture.Core.Mediator;

namespace Core.Application.Tests.Transaction;

public class TransactionScopeBehaviorTests
{
    private sealed record TransactionalCommand : IRequest<string>, ITransactionalRequest;

    [Fact]
    public async Task Handler_runs_inside_an_ambient_transaction_that_is_gone_afterwards()
    {
        TransactionScopeBehavior<TransactionalCommand, string> behavior = new();
        System.Transactions.Transaction? observed = null;

        string response = await behavior.Handle(
            new TransactionalCommand(),
            async _ =>
            {
                await Task.Yield();
                observed = System.Transactions.Transaction.Current;
                return "ok";
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal("ok", response);
        Assert.NotNull(observed);
        Assert.Null(System.Transactions.Transaction.Current);
    }

    [Fact]
    public async Task Exceptions_propagate_and_no_transaction_leaks()
    {
        TransactionScopeBehavior<TransactionalCommand, string> behavior = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TransactionalCommand(),
                _ => throw new InvalidOperationException("boom"),
                TestContext.Current.CancellationToken
            )
        );

        Assert.Null(System.Transactions.Transaction.Current);
    }
}
