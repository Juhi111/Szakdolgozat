using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class PostgreSqlQueryService
    {
        private readonly PostgreSqlQueryParser _parser = new();

        public SqlParseSummary ParseSummary(string sql) { 
            if(string.IsNullOrWhiteSpace(sql)) {
                return new SqlParseSummary { ErrorMessage = "SQL query is empty." };
            }
            try {
                var result = _parser.Parse(sql);
                if (result.Stmts.Count == 0)
                {
                    return new SqlParseSummary
                    {
                        ErrorMessage = "Az SQL-szöveg nem tartalmaz utasítást."
                    };
                }
                return new SqlParseSummary { StatementCount = result.Stmts.Count };
            } catch(FormatException ex) {
                return new SqlParseSummary { ErrorMessage = ex.Message };
            }
        }
    }
}
