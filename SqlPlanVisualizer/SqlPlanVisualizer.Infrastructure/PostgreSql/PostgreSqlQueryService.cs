using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    /// <summary>
    /// Összehangolja az SQL feldolgozását, az AST bejárását
    /// és az elemzési szabályokhoz szükséges előfordulások összegyűjtését.
    /// </summary>
    public class PostgreSqlQueryService
    {
        /// <summary>
        /// Az SQL-szöveget absztrakt szintaxisfává alakító parser.
        /// </summary>
        private readonly PostgreSqlQueryParser _parser = new();

        /// <summary>
        /// Az AST csomópontjainak és bejárási kontextusának előállítását végzi.
        /// </summary>
        private readonly PostgreSqlAstWalker _walker = new();

        /// <summary>
        /// A bejárt csomópontokból az elemzéshez szükséges előfordulásokat gyűjtő komponensek.
        /// </summary>
        private readonly List<ISqlAstCollector> _collectors = new()
        {
            new WildcardProjectionCollector(),
            new DistinctCollector(),
            new NullComparisonCollector(),
            new LimitCollector(),
            new LikeCollector(),
            new FunctionOnColumnCollector(),
            new CastOnColumnCollector(),
            new ArithmeticOnColumnCollector(),
            new NotInSubqueryCollector(),
            new YearExtractionCollector(),
            new ConcatenationCollector(),
            new OptionalParameterFilterCollector(),
            new CaseInPredicateCollector(),
            new NotInWithNullLiteralCollector(),
            new CountForExistenceCollector(),
            new RowFilterInHavingCollector(),
            new PossibleCartesianProductCollector(),
            new OffsetCollector(),
            new UnionDeduplicationCollector(),
            new NaturalJoinCollector(),
            new RandomOrderingCollector()
        };

        /// <summary>
        /// Feldolgozza az SQL-szöveget, és utasításonként összefoglalót készít.
        /// A gyökérszintű SELECT utasítások teljes részfáján futtatja a collectorokat;
        /// más utasításoknál csak a sorszámot és a típust rögzíti.
        /// Az üres bemenetet és a parser formátumhibáját hibaüzenetként adja vissza.
        /// </summary>
        /// <param name="sql">A feldolgozandó PostgreSQL SQL-szöveg.</param>
        /// <returns>Az utasítások összefoglalóit vagy a feldolgozás hibaüzenetét tartalmazó eredmény.</returns>
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
                    else
                    {
                        statementSummary.StatementType = res.Stmt.NodeCase.ToString();
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