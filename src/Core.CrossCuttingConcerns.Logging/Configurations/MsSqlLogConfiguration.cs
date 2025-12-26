namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Configurations;
public class MsSqlLogConfiguration
{
    public string TableName { get; set; }
    public bool AutoCreateSqlTable { get; set; }
    public string ConnectionString { get; set; }

    public MsSqlLogConfiguration()
    {
        TableName = "Logs";
        AutoCreateSqlTable = true;
        ConnectionString = "data source=NEPTUN\\DVLP2008;initial catalog=TestDb;persist security info=False;user id=sa;password=test^3;";
    }

    public MsSqlLogConfiguration(string tableName, bool autoCreateSqlTable,string connectionString)
    {
        TableName = tableName;
        AutoCreateSqlTable = autoCreateSqlTable;
        ConnectionString = connectionString;
    }
}
