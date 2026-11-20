using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Models.Database
{
    public sealed class DatabaseConnectionInfo
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ServerVersion { get; set; } = string.Empty;
    }
}
