using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator;
using NArchitecture.Core.Mediator.DependencyInjection;
using NArchitecture.Core.Mediator.Exceptions;

namespace Core.Mediator.Tests;

public class MediatorTests
{
    private static ServiceProvider build(Action<MediatorConfiguration>? configure = null)
    {
        ServiceCollection services = new();
        services.AddSingleton<Trace>();
        services.AddSingleton<FireHandler>();
        services.AddSingleton<IRequestHandler<Fire, Unit>>(sp => sp.GetRequiredService<FireHandler>());
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<MediatorTests>();
            configure?.Invoke(cfg);
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Send_resolves_handler_and_returns_response()
    {
        await using ServiceProvider sp = build();
        IMediator mediator = sp.GetRequiredService<IMediator>();

        string response = await mediator.Send(new Ping("Ping"), TestContext.Current.CancellationToken);

        Assert.Equal("Ping Pong", response);
    }

    [Fact]
    public async Task Send_unit_request_uses_default_interface_implementation()
    {
        await using ServiceProvider sp = build();
        ISender sender = sp.GetRequiredService<ISender>();

        await sender.Send(new Fire(), TestContext.Current.CancellationToken);

        Assert.Equal(1, sp.GetRequiredService<FireHandler>().Calls);
    }

    [Fact]
    public async Task Send_without_handler_throws_HandlerNotFoundException()
    {
        await using ServiceProvider sp = build();
        IMediator mediator = sp.GetRequiredService<IMediator>();

        HandlerNotFoundException ex = await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
            mediator.Send(new Orphan(), TestContext.Current.CancellationToken)
        );

        Assert.Equal(typeof(Orphan), ex.RequestType);
        Assert.Contains(nameof(Orphan), ex.Message);
    }

    [Fact]
    public async Task Behaviors_run_in_registration_order_and_unwind_in_reverse()
    {
        await using ServiceProvider sp = build(cfg =>
        {
            cfg.AddOpenBehavior(typeof(BehaviorA<,>));
            cfg.AddOpenBehavior(typeof(BehaviorB<,>));
            cfg.AddOpenBehavior(typeof(BehaviorC<,>));
        });

        await sp.GetRequiredService<IMediator>().Send(new Ping("x"), TestContext.Current.CancellationToken);

        Assert.Equal(["A>", "B>", "C>", "<C", "<B", "<A"], sp.GetRequiredService<Trace>().Entries);
    }

    [Fact]
    public async Task Constrained_open_behavior_only_applies_to_requests_satisfying_the_constraint()
    {
        await using ServiceProvider sp = build(cfg =>
        {
            cfg.AddOpenBehavior(typeof(MarkerOnlyBehavior<,>));
            cfg.AddOpenBehavior(typeof(BehaviorA<,>));
        });
        IMediator mediator = sp.GetRequiredService<IMediator>();
        Trace trace = sp.GetRequiredService<Trace>();

        await mediator.Send(new Ping("x"), TestContext.Current.CancellationToken);
        Assert.Equal(["A>", "<A"], trace.Entries);

        trace.Entries.Clear();
        await mediator.Send(new Marked("x"), TestContext.Current.CancellationToken);
        Assert.Equal(["Marker", "A>", "<A"], trace.Entries);
    }

    [Fact]
    public async Task Next_without_token_forwards_the_token_the_request_was_sent_with()
    {
        await using ServiceProvider sp = build(cfg => cfg.AddOpenBehavior(typeof(BehaviorA<,>)));
        using CancellationTokenSource cts = new();

        CancellationToken observed = await sp.GetRequiredService<IMediator>().Send(new TokenProbe(), cts.Token);

        Assert.Equal(cts.Token, observed);
    }

    [Fact]
    public async Task Cancelled_token_propagates_to_the_handler()
    {
        await using ServiceProvider sp = build();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        CancellationToken observed = await sp.GetRequiredService<IMediator>().Send(new TokenProbe(), cts.Token);

        Assert.True(observed.IsCancellationRequested);
    }

    [Fact]
    public async Task CreateStream_yields_handler_items()
    {
        await using ServiceProvider sp = build();

        List<int> items = [];
        await foreach (
            int item in sp.GetRequiredService<IMediator>().CreateStream(new CountUp(3), TestContext.Current.CancellationToken)
        )
            items.Add(item);

        Assert.Equal([0, 1, 2], items);
    }

    [Fact]
    public async Task Stream_behaviors_wrap_the_stream()
    {
        await using ServiceProvider sp = build(cfg => cfg.AddOpenStreamBehavior(typeof(DoublingStreamBehavior<,>)));

        List<int> items = [];
        await foreach (
            int item in sp.GetRequiredService<IMediator>().CreateStream(new CountUp(3), TestContext.Current.CancellationToken)
        )
            items.Add(item);

        Assert.Equal([0, 2, 4], items);
        Assert.Equal(["Stream>", "<Stream"], sp.GetRequiredService<Trace>().Entries);
    }

    [Fact]
    public async Task CreateStream_stops_when_cancelled()
    {
        await using ServiceProvider sp = build();
        using CancellationTokenSource cts = new();

        List<int> items = [];
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (int item in sp.GetRequiredService<IMediator>().CreateStream(new CountUp(100), cts.Token))
            {
                items.Add(item);
                if (item == 2)
                    cts.Cancel();
            }
        });

        Assert.Equal([0, 1, 2], items);
    }

    [Fact]
    public async Task CreateStream_without_handler_throws_HandlerNotFoundException()
    {
        await using ServiceProvider sp = build();

        await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
        {
            await foreach (
                int _ in sp.GetRequiredService<IMediator>()
                    .CreateStream(new OrphanStream(), TestContext.Current.CancellationToken)
            ) { }
        });
    }

    [Fact]
    public void Mediator_is_transient_by_default()
    {
        using ServiceProvider sp = build();

        Assert.NotSame(sp.GetRequiredService<IMediator>(), sp.GetRequiredService<IMediator>());
    }

    [Fact]
    public void Configured_lifetime_is_respected()
    {
        using ServiceProvider sp = build(cfg => cfg.Lifetime = ServiceLifetime.Scoped);
        using IServiceScope scope = sp.CreateScope();

        Assert.Same(scope.ServiceProvider.GetRequiredService<IMediator>(), scope.ServiceProvider.GetRequiredService<IMediator>());
        Assert.Same(scope.ServiceProvider.GetRequiredService<IMediator>(), scope.ServiceProvider.GetRequiredService<ISender>());
    }

    [Fact]
    public void AddOpenBehavior_rejects_closed_or_unrelated_types()
    {
        MediatorConfiguration cfg = new();

        Assert.Throws<ArgumentException>(() => cfg.AddOpenBehavior(typeof(BehaviorA<Ping, string>)));
        Assert.Throws<ArgumentException>(() => cfg.AddOpenBehavior(typeof(List<>)));
        Assert.Throws<ArgumentException>(() => cfg.AddOpenStreamBehavior(typeof(BehaviorA<,>)));
    }

    private sealed record OrphanStream : IStreamRequest<int>;
}
