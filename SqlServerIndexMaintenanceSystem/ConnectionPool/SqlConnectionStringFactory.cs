using System.Text.RegularExpressions;
using CredentialsManager;

namespace SqlServerIndexMaintenanceSystem.ConnectionPool;

public class SqlConnectionStringFactory(string clientDatabaseTemplate, CredentialsStorage credentials, ILogger<SqlConnectionStringFactory> logger)
{
    public string CreateConnectionString(string server, string database, int? poolSize, bool integratedSecurity)
    {
        var cleanTemplate = clientDatabaseTemplate;

        cleanTemplate = new Regex("Pooling=\\w+;*", RegexOptions.IgnoreCase).Replace(cleanTemplate, "");
        cleanTemplate = new Regex("MaxPoolSize=\\d+;*", RegexOptions.IgnoreCase).Replace(cleanTemplate, "");
        cleanTemplate = new Regex("MinPoolSize=\\d+;*", RegexOptions.IgnoreCase).Replace(cleanTemplate, "");
        cleanTemplate = cleanTemplate.TrimEnd(';') + ";";

        cleanTemplate = string.Format(cleanTemplate, server, database);

        if (poolSize.HasValue)
        {
            if (poolSize == 0)
            {
                cleanTemplate += "Pooling=False;";
            }
            else
            {
                cleanTemplate += $"Pooling=True;Max Pool Size={poolSize};";
            }
        }

        if (!integratedSecurity)
        {
            var credentialDict = credentials.LoadCredentials().ToDictionary(c => c.Server.ToLower());

            if (credentialDict.TryGetValue(server.ToLower(), out var credential))
            {
                logger.LogInformation($"Using SQL Server authentication for {server} with username {credential.Username} in order to connect to database {database}");

                cleanTemplate = new Regex("Trusted_Connection=true;", RegexOptions.IgnoreCase)
                    .Replace(cleanTemplate, "");

                cleanTemplate += $"User Id={credential.Username};Password={credential.Password};";
            }
            else
            {
                throw new Exception($"Failed to load SQL Server credentials for {server}");
            }
        }


        return cleanTemplate;
    }
}