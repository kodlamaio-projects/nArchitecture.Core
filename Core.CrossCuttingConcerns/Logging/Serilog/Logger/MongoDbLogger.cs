using Core.CrossCuttingConcerns.Logging.Serilog.ConfigurationModels;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog;

namespace Core.CrossCuttingConcerns.Logging.Serilog.Logger;

public class MongoDbLogger : LoggerServiceBase
{
    public MongoDbLogger(IConfiguration configuration)
    {
        const string configurationSection = "SeriLogConfigurations:MongoDbConfiguration";
        MongoDbConfiguration logConfiguration =
            configuration.GetSection(configurationSection).Get<MongoDbConfiguration>()
            ?? throw new NullReferenceException($"\"{configurationSection}\" section cannot found in configuration.");

        Logger = new LoggerConfiguration().WriteTo
            .MongoDBBson(cfg =>
            {
                MongoClient client = new(logConfiguration.ConnectionString);
                IMongoDatabase? mongoDbInstance = client.GetDatabase(logConfiguration.Collection);
                cfg.SetMongoDatabase(mongoDbInstance);
            })
            .CreateLogger();
    }
}
