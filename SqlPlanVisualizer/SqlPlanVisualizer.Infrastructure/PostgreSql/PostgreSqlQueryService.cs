using SqlPlanVisualizer.Core.Models;
using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class PostgreSqlQueryService
    {
        private readonly PostgreSqlQueryParser _parser = new();

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
                        CollectWildcardProjections(selectStatement, "Fő lekérdezés", statementSummary.WildcardProjections);
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

        private void CollectWildcardProjections(PgSqlParser.SelectStmt selectStatement, string location, List<WildcardProjectionOccurrence> occurrences)
        {
            foreach (var target in selectStatement.TargetList)
            {
                var columnRef = target.ResTarget?.Val?.ColumnRef;
                if (columnRef != null)
                {
                    var qualifierParts = new List<string>();                    

                    foreach (var field in columnRef.Fields)
                    {
                        if(field.String != null)
                            qualifierParts.Add(field.String.Sval);
                        if (field.AStar != null)
                        {
                            occurrences.Add(new WildcardProjectionOccurrence
                            {
                                Location = location,
                                Qualifier = qualifierParts.Count > 0 ? string.Join(".", qualifierParts) : null
                            });
                            break;
                        }
                    }
                }
            }
            if(selectStatement.Larg != null)
            {
                CollectWildcardProjections(selectStatement.Larg, $"{location} / bal ág", occurrences);
            }

            if (selectStatement.Rarg != null)
            {
                CollectWildcardProjections(selectStatement.Rarg, $"{location} / jobb ág", occurrences);
            }

            int fromIndex = 1;
            foreach (var fromItem in selectStatement.FromClause)
            {
                CollectWildCardsInFromItem(fromItem, $"{location} / FROM[{fromIndex}]", occurrences);
                fromIndex++;
            }

            if (selectStatement.WithClause != null)
            {
                foreach (var cte in selectStatement.WithClause.Ctes)
                {
                    var cteSelect = cte.CommonTableExpr?.Ctequery?.SelectStmt;
                    if (cteSelect != null)
                    {
                        CollectWildcardProjections(cteSelect, $"{location} / CTE[{cte.CommonTableExpr.Ctename}]", occurrences);
                    }
                }
            }
        }

        private void CollectWildCardsInFromItem(Node fromItem, string location, List<WildcardProjectionOccurrence> occurrences) { 
            var subSelect = fromItem.RangeSubselect?.Subquery?.SelectStmt;
            if (subSelect != null)
            {
                CollectWildcardProjections(subSelect, location + " / allekérdezés", occurrences);
            }

            var joinExpr = fromItem.JoinExpr;
            if (joinExpr != null)
            {
                if (joinExpr.Larg != null)
                {
                    CollectWildCardsInFromItem(joinExpr.Larg, $"{location} / JOIN BAL", occurrences);
                }
                if (joinExpr.Rarg != null)
                {
                    CollectWildCardsInFromItem(joinExpr.Rarg, $"{location} / JOIN JOBB", occurrences);
                }
            }
        }
    }
}
