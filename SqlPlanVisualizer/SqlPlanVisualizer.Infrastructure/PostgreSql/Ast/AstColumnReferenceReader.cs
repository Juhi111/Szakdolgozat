using PgSqlParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    public static class AstColumnReferenceReader
    {
        public static bool TryRead(ColumnRef column, out string columnName, out string? qualifier)
        {
            columnName = string.Empty;
            qualifier = null;

            if (column is null)
                throw new ArgumentNullException(nameof(column));

            if (column.Fields.Count == 0)
                return false;

            var parts = new List<string>();

            foreach (var field in column.Fields)
            {                
                if (field.String is null)
                    return false;

                parts.Add(field.String.Sval);
            }
            
            columnName = parts[parts.Count - 1];
            
            if (parts.Count > 1)
            {
                qualifier = string.Join(
                    ".",
                    parts.GetRange(0, parts.Count - 1));
            }
            return true;
        }
    }
}
