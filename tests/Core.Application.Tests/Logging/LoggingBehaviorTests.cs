using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging;
using NArchitecture.Core.Mediator;
using ILogger = NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction.ILogger;

namespace Core.Application.Tests.Logging;

public class LoggingBehaviorTests
{
    private sealed record LoggedCommand(string Payload) : IRequest<string>, ILoggableRequest;

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Information { get; } = [];

        void ILogger.Information(string message) => Information.Add(message);

        public void Trace(string message) { }

        public void Critical(string message) { }

        public void Warning(string message) { }

        public void Debug(string message) { }

        public void Error(string message) { }
    }

    [Fact]
    public async Task Logs_request_type_payload_and_user_then_calls_handler()
    {
        CapturingLogger logger = new();
        DefaultHttpContext context = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "halit")], "test")),
        };
        LoggingBehavior<LoggedCommand, string> behavior = new(logger, new HttpContextAccessor { HttpContext = context });

        string response = await behavior.Handle(
            new LoggedCommand("hello"),
            _ => Task.FromResult("ok"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal("ok", response);
        LogDetail detail = JsonSerializer.Deserialize<LogDetail>(Assert.Single(logger.Information))!;
        Assert.Equal(nameof(LoggedCommand), detail.MethodName);
        Assert.Equal("halit", detail.User);
        Assert.Equal(nameof(LoggedCommand), Assert.Single(detail.Parameters).Type);
    }

    [Fact]
    public async Task Null_HttpContext_logs_unknown_user()
    {
        CapturingLogger logger = new();
        LoggingBehavior<LoggedCommand, string> behavior = new(logger, new HttpContextAccessor { HttpContext = null });

        await behavior.Handle(new LoggedCommand("hello"), _ => Task.FromResult("ok"), TestContext.Current.CancellationToken);

        LogDetail detail = JsonSerializer.Deserialize<LogDetail>(Assert.Single(logger.Information))!;
        Assert.Equal("?", detail.User);
    }
}
