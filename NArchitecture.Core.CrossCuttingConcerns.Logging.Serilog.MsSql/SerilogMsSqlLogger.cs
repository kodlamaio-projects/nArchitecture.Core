using NArchitecture.Core.CrossCuttingConcerns.Logging.Configurations;
using Serilog;
using Serilog.Sinks.MSSqlServer;

namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog.MsSql;
public class SerilogMsSqlLogger : SerilogLoggerServiceBase
{
    public SerilogMsSqlLogger(MsSqlLogConfiguration configuration) : base(logger:null!)
    {
        MSSqlServerSinkOptions sinkOptions = new()
        {
            TableName = configuration.TableName,
            AutoCreateSqlDatabase = configuration.AutoCreateSqlTable
        };

        ColumnOptions columnOptions = new();

        Logger = new LoggerConfiguration()
            .WriteTo.MSSqlServer(
               connectionString:configuration.ConnectionString,
               sinkOptions:sinkOptions,
               columnOptions: columnOptions
               
            )
            .CreateLogger();
    }
}
