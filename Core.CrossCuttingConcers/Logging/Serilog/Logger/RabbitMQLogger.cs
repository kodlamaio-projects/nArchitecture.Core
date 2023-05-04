using Core.CrossCuttingConcerns.Logging.Serilog.ConfigurationModels;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.RabbitMQ;
using Serilog.Sinks.RabbitMQ.Sinks.RabbitMQ;

namespace Core.CrossCuttingConcerns.Logging.Serilog.Logger;

public class RabbitMQLogger : LoggerServiceBase
{
    public RabbitMQLogger(IConfiguration configuration)
    {
        const string configurationSection = "SeriLogConfigurations:RabbitMQConfiguration";
        RabbitMQConfiguration rabbitMQConfiguration =
            configuration.GetSection(configurationSection).Get<RabbitMQConfiguration>()
            ?? throw new NullReferenceException($"\"{configurationSection}\" section cannot found in configuration.");

        RabbitMQClientConfiguration config =
            new()
            {
                Port = rabbitMQConfiguration.Port,
                DeliveryMode = RabbitMQDeliveryMode.Durable,
                Exchange = rabbitMQConfiguration.Exchange,
                Username = rabbitMQConfiguration.Username,
                Password = rabbitMQConfiguration.Password,
                ExchangeType = rabbitMQConfiguration.ExchangeType,
                RouteKey = rabbitMQConfiguration.RouteKey
            };
        rabbitMQConfiguration.Hostnames.ForEach(config.Hostnames.Add);

        Logger = new LoggerConfiguration().WriteTo
            .RabbitMQ(
                (clientConfiguration, sinkConfiguration) =>
                {
                    clientConfiguration.From(config);
                    sinkConfiguration.TextFormatter = new JsonFormatter();
                }
            )
            .CreateLogger();
    }
}
