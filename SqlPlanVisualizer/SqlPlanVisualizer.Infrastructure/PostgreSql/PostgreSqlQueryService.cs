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
                CollectWildcardsInExpression(target.ResTarget?.Val, $"{location} / SELECT-kifejezés", occurrences);

            }

            CollectWildcardsInExpression(selectStatement.WhereClause, $"{location} / WHERE-kifejezés", occurrences);

            CollectWildcardsInExpression(selectStatement.HavingClause, $"{location} / HAVING-kifejezés", occurrences);

            CollectWildcardsInExpression(selectStatement.LimitCount, $"{location} / LIMIT-kifejezés", occurrences);

            CollectWildcardsInExpression(selectStatement.LimitOffset, $"{location} / OFFSET-kifejezés", occurrences);

            int sortIndex = 1;
            foreach (var sortClause in selectStatement.SortClause)
            {
                CollectWildcardsInExpression(sortClause.SortBy?.Node, $"{location} / ORDER BY[{sortIndex}]", occurrences);
                sortIndex++;
            }

            if (selectStatement.Larg != null)
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
                        CollectWildcardProjections(cteSelect, $"{location} / CTE[{cte.CommonTableExpr?.Ctename}]", occurrences);
                    }
                }
            }

            int groupIndex = 1;
            foreach (var groupClause in selectStatement.GroupClause)
            {
                CollectWildcardsInExpression(groupClause, $"{location} / GROUP BY[{groupIndex}]", occurrences);
                groupIndex++;
            }

            int distinctIndex = 1;
            foreach (var distinctClause in selectStatement.DistinctClause)
            {
                CollectWildcardsInExpression(distinctClause, $"{location} / DISTINCT ON[{distinctIndex}]", occurrences);
                distinctIndex++;
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
                CollectWildcardsInExpression(joinExpr.Quals, $"{location} / JOIN ON", occurrences);
            }            
        }

        private void CollectWildcardsInExpression(Node? expression, string location, List<WildcardProjectionOccurrence> occurrences)
        {
            if (expression == null)
                return;

            var subselect = expression.SubLink?.Subselect?.SelectStmt;
            if (subselect != null)
            {
                CollectWildcardProjections(subselect, $"{location} / allekérdezés", occurrences);
            }

            var boolExpr = expression.BoolExpr;
            if (boolExpr != null)
            {
                int argumentIndex = 1;
                foreach (var arg in boolExpr.Args)
                {
                    CollectWildcardsInExpression(arg, $"{location} / feltétel[{argumentIndex}]", occurrences);
                    argumentIndex++;
                }
            }

            var expr = expression.AExpr;
            if (expr != null)
            {
                CollectWildcardsInExpression(expr.Lexpr, $"{location} / bal operandus", occurrences);
                CollectWildcardsInExpression(expr.Rexpr, $"{location} / jobb operandus", occurrences);
            }

            var funCall = expression.FuncCall;
            if (funCall != null)
            {
                int argumentumIndex = 1;
                foreach (var arg in funCall.Args)
                {
                    CollectWildcardsInExpression(arg, $"{location} / függvényargumentum[{argumentumIndex}]", occurrences);
                    argumentumIndex++;
                }

                CollectWildcardsInExpression(funCall.AggFilter, $"{location} / FILTER-feltétel", occurrences);
            }

            var typeCast = expression.TypeCast;
            if (typeCast != null)
            {
                CollectWildcardsInExpression(typeCast.Arg, $"{location} / típuskonverzió", occurrences);
            }

            var caseExpr = expression.CaseExpr;
            if (caseExpr != null)
            {
                CollectWildcardsInExpression(caseExpr.Arg, $"{location} / CASE alapérték", occurrences);

                int branchIndex = 1;

                foreach (var arg in caseExpr.Args)
                {
                    if (arg.CaseWhen != null)
                    {
                        CollectWildcardsInExpression(arg.CaseWhen.Expr, $"{location} / CASE WHEN[{branchIndex}]", occurrences);
                        CollectWildcardsInExpression(arg.CaseWhen.Result, $"{location} / CASE THEN[{branchIndex}]", occurrences);
                    }
                    branchIndex++;
                }
                CollectWildcardsInExpression(caseExpr.Defresult, $"{location} / CASE ELSE", occurrences);
            }

            var coalesceExpr = expression.CoalesceExpr;
            if (coalesceExpr != null)
            {
                int argumentIndex = 1;
                foreach (var arg in coalesceExpr.Args)
                {
                    CollectWildcardsInExpression(arg, $"{location} / COALESCE argumentum[{argumentIndex}]", occurrences);
                    argumentIndex++;
                }
            }

            var nullTest = expression.NullTest;
            if (nullTest != null)
            {
                CollectWildcardsInExpression(nullTest.Arg, $"{location} / NULL-vizsgálat", occurrences);
            }

            var booleanTest = expression.BooleanTest;
            if (booleanTest != null)
            {
                CollectWildcardsInExpression(booleanTest.Arg, $"{location} / logikai vizsgálat", occurrences);
            }

            var groupingSet = expression.GroupingSet;
            if (groupingSet != null)
            {
                int elementIndex = 1;
                foreach (var arg in groupingSet.Content)
                {
                    CollectWildcardsInExpression(arg, $"{location} / csoportosítási elem[{elementIndex}]", occurrences);
                    elementIndex++;
                }
            }
        }
    }
}
