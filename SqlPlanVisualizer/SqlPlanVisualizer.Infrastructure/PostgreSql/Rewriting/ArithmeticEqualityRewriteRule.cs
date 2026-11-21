using System.Globalization;
using PgSqlParser;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting
{
    /// <summary>A WHERE oszlop +/- egész = egész alakját írja át AST alapján, feltételes javaslatként.</summary>
    public sealed class ArithmeticEqualityRewriteRule : ISqlRewriteRule
    {
        private readonly PostgreSqlQueryParser _parser = new();

        public IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            var parsed = _parser.Parse(sql);
            if (parsed.Stmts.Count != 1)
                return Array.Empty<SqlRewriteSuggestion>();

            var select = parsed.Stmts[0].Stmt.SelectStmt;
            if (select is null || select.Op != SetOperation.SetopNone || select.WithClause is not null ||
                select.IntoClause is not null || select.LockingClause.Count != 0 || select.FromClause.Count != 1 ||
                select.FromClause[0].RangeVar is null)
                return Array.Empty<SqlRewriteSuggestion>();

            var predicates = ReadPredicates(select.WhereClause).ToList();
            var suggestions = new List<SqlRewriteSuggestion>();
            for (int i = 0; i < predicates.Count; i++)
            {
                var comparison = predicates[i];
                if (ReadOperator(comparison) != "=" || comparison.Lexpr?.AExpr is not A_Expr arithmetic ||
                    ReadOperator(arithmetic) is not ("+" or "-") || arithmetic.Lexpr?.ColumnRef is not ColumnRef column ||
                    column.Fields.Count == 0 || column.Fields.Any(field => field.String is null) ||
                    !TryReadInteger(arithmetic.Rexpr, out int delta) || !TryReadInteger(comparison.Rexpr, out int target))
                    continue;

                // Csak az int32 literáltartományon belüli eredményt állítjuk elő.
                long adjusted = ReadOperator(arithmetic) == "+" ? (long)target - delta : (long)target + delta;
                if (adjusted < int.MinValue || adjusted > int.MaxValue)
                    continue;

                var candidate = parsed.Clone();
                var predicate = ReadPredicates(candidate.Stmts[0].Stmt.SelectStmt.WhereClause).ElementAt(i);
                predicate.Lexpr = new Node { ColumnRef = column.Clone() };
                // A negatív konstansok AST-ábrázolását is a parserre bízzuk.
                var constantSql = "SELECT 1 WHERE 1 = " + adjusted.ToString(CultureInfo.InvariantCulture);
                predicate.Rexpr = _parser.Parse(constantSql).Stmts[0].Stmt.SelectStmt.WhereClause.AExpr.Rexpr.Clone();
                var deparsed = Parser.Deparse(candidate);
                if (deparsed.Error is not null || string.IsNullOrWhiteSpace(deparsed.Value))
                    continue;

                _parser.Parse(deparsed.Value);
                suggestions.Add(new SqlRewriteSuggestion
                {
                    RuleId = "ARITHMETIC_EQUALITY_REWRITE",
                    Title = "Az oszlopon végzett összeadás vagy kivonás áthelyezése",
                    OriginalSql = sql,
                    SuggestedSql = deparsed.Value,
                    Explanation = "A WHERE egyenlőségi feltételében a konstanssal végzett műveletet áthelyeztük a jobb oldalra. " +
                        "Így az oszlop önmagában szerepelhet a szűrésben, ami segítheti egy megfelelő index használatát. " +
                        "Minden javaslat az eredeti SQL egyetlen feltételét módosítja; a változatok nincsenek összevonva. " +
                        "Az SQL-t az AST-ből állítottuk elő, ezért a formázás változhat, a megjegyzések kimaradnak.",
                    Preconditions = "Ellenőrizd, hogy az oszlop beépített egész típusú, és a műveletek a PostgreSQL szokásos operátorait használják. " +
                        "Az eredeti összeadás vagy kivonás a vizsgált sorokon nem okozhat túlcsordulást; az átírás egyébként a hibaviselkedést is megváltoztathatja. " +
                        "Egyéni típusokhoz, operátorokhoz és lebegőpontos adatokhoz ez a javaslat nem igazolt. " +
                        "Az alkalmazás jelenleg nem kér le oszloptípust vagy értéktartományt. Használat előtt ellenőrizd az eredményeket, " +
                        "majd azonos körülmények között hasonlítsd össze a végrehajtási terveket."
                });
            }
            return suggestions;
        }

        private static string? ReadOperator(A_Expr expression) =>
            expression.Kind == A_Expr_Kind.AexprOp && expression.Name.Count == 1 ? expression.Name[0].String?.Sval : null;

        private static bool TryReadInteger(Node? node, out int value)
        {
            value = 0;
            if (node?.AConst is not A_Const constant || constant.Isnull || constant.Ival is null)
                return false;
            value = constant.Ival.Ival;
            return true;
        }

        private static IEnumerable<A_Expr> ReadPredicates(Node? node)
        {
            if (node?.AExpr is A_Expr expression)
                yield return expression;
            else if (node?.BoolExpr is BoolExpr boolean && boolean.Boolop == BoolExprType.AndExpr)
                foreach (var argument in boolean.Args)
                    foreach (var predicate in ReadPredicates(argument))
                        yield return predicate;
        }
    }
}
