using System;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Desktop.Services
{
    /// <summary>
    /// Az alkalmazás futása alatt tárolja a kiválasztott kapcsolat adatait.
    /// </summary>
    public sealed class DatabaseSession
    {
        private DatabaseConnectionOptions? _options;
        private string? _password;

        public bool HasConfiguration => _options is not null;

        public void Set(DatabaseConnectionOptions options, string password)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(password);

            _options = CopyOptions(options);
            _password = password;
        }

        public (DatabaseConnectionOptions Options, string Password)
            GetConnectionDetails()
        {
            if (_options is null || _password is null)
            {
                throw new InvalidOperationException(
                    "Nincs beállított adatbázis-kapcsolat.");
            }

            return (CopyOptions(_options), _password);
        }

        public void Clear()
        {
            _options = null;
            _password = null;
        }

        private static DatabaseConnectionOptions CopyOptions(
            DatabaseConnectionOptions source)
        {
            return new DatabaseConnectionOptions
            {
                Host = source.Host,
                Port = source.Port,
                Database = source.Database,
                Username = source.Username,
                Schema = source.Schema,
                TlsMode = source.TlsMode
            };
        }
    }
}