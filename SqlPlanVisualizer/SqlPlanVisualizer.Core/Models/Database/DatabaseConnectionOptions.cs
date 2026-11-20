
using SqlPlanVisualizer.Core.Enums;

namespace SqlPlanVisualizer.Core.Models.Database
{
    public sealed class DatabaseConnectionOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Schema { get; set; } = "public";
        public DatabaseTlsMode TlsMode { get; set; } = DatabaseTlsMode.VerifyFull;
    }
}
