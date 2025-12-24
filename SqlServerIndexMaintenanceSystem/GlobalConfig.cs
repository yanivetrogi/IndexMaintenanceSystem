using System.Text;

namespace SqlServerIndexMaintenanceSystem
{
    public class GlobalConfig
    {
        public int? ExecutionIntervalSeconds { get; set; }
        public string? CredentialsFilePath { get; set; }

        public override string ToString()
        {
            return @$"Global config:
    ExecutionIntervalSeconds: {ExecutionIntervalSeconds};
    CredentialsFilePath: {CredentialsFilePath}";
        }
    }
}