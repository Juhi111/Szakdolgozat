using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class PostgreSqlQueryService
    {
        private readonly PostgreSqlQueryParser _parser = new();
        private readonly PostgreSqlAstWalker _walker = new();

        private readonly List<ISqlAstCollector> _collectors = new()
        {
            new WildcardProjectionCollector(),
            new DistinctCollector(),
            new NullComparisonCollector(),
            new LimitCollector(),
            new LikeCollector()
        };

        public SqlParseSummary ParseSummary(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return new SqlParseSummary { ErrorMessage = "SQL query is empty." };
            }
            try
            {
                var summary = new SqlParseSummary();
                var result = _parser.Parse(sql);

                if (result.Stmts.Count == 0)
                {
                    return new SqlParseSummary
                    {
                        ErrorMessage = "Az SQL-szöveg nem tartalmaz utasítást."
                    };
                }

                int statementIndex = 1;

                foreach (var res in result.Stmts)
                {
                    var statementSummary = new SqlStatementSummary
                    {
                        StatementIndex = statementIndex
                    };

                    var selectStatement = res.Stmt.SelectStmt;
                    if (selectStatement != null)
                    {
                        statementSummary.StatementType = "SELECT";

                        _walker.Walk(res.Stmt, statementIndex, entry =>
                        {
                            foreach (var collector in _collectors)
                            {
                                collector.Collect(entry, statementSummary);
                            }
                        });
                    }
                    summary.Statements.Add(statementSummary);
                    statementIndex++;
                }
                summary.StatementCount = summary.Statements.Count;
                return summary;

            }
            catch (FormatException ex)
            {
                return new SqlParseSummary { ErrorMessage = ex.Message };
            }
        }        
    }
}