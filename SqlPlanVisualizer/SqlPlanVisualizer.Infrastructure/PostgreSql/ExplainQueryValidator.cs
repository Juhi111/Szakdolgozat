using PgSqlParser;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public sealed class ExplainQueryValidator
    {
        private readonly PostgreSqlQueryParser _parser = new();
        private readonly PostgreSqlAstWalker _walker = new();

        public void Validate(string sql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);

            var result = _parser.Parse(sql);

            if (result.Stmts.Count != 1)
                throw new ArgumentException("A tervlekéréshez pontosan egy SQL-utasítást adj meg.", nameof(sql));

            var statement = result.Stmts[0].Stmt;

            if (statement.SelectStmt is null)
                throw new ArgumentException("Jelenleg csak SELECT utasításhoz kérhető végrehajtási terv.", nameof(sql));

            _walker.Walk(statement, 1, entry =>
            {
                if (entry.Node is SelectStmt select)
                {
                    if (select.IntoClause is not null)
                        throw new ArgumentException("A SELECT INTO utasítás nem támogatott.", nameof(sql));

                    if (select.LockingClause.Count > 0)
                        throw new ArgumentException("A sorzárolást kérő SELECT utasítás nem támogatott.", nameof(sql));
                }

                if (entry.Node is CommonTableExpr cte && cte.Ctequery?.SelectStmt is null)
                    throw new ArgumentException("A WITH részben csak SELECT lekérdezés szerepelhet.", nameof(sql));
            });
        }
    }
}