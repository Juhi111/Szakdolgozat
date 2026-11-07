using PgSqlParser;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors;

/// <summary>A meglévő felismerőket használja a felső szintű WHERE/HAVING szerkezeti párosításához.</summary>
public class PredicateCorrelationCollector : ISqlAstCollector
{
    private readonly AstContextResolver resolver = new();
    private readonly ISqlAstCollector[] recognizers =
    {
        new FunctionOnColumnCollector(), new LikeCollector(), new CastOnColumnCollector(),
        new ArithmeticOnColumnCollector(), new YearExtractionCollector(), new ConcatenationCollector(),
        new OptionalParameterFilterCollector(), new CaseInPredicateCollector(), new RowFilterInHavingCollector()
    };

    public void Collect(AstVisitEntry entry, SqlStatementSummary summary)
    {
        var query = summary.TopLevelQuery;
        if (query is null || query.IsSetOperation || !query.HasOnlyDirectRelations || query.Relations.Count != 1 ||
            entry.Node is not (FuncCall or A_Expr or TypeCast or BoolExpr or CaseExpr) ||
            resolver.GetClause(entry.Context) is not (AstClause.Where or AstClause.Having)) return;

        var select = resolver.FindSelectContext(entry.Context);
        for (var ancestor = select?.Parent; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor.Node is SelectStmt) return;
        if (select is null) return;

        string? signature = ExpressionFingerprint.Read(entry.Node, query.Relations[0]);
        if (signature is null) return;

        // A szabályok felismerési feltételei egy helyen maradnak: a meglévő collectorokban.
        var detected = new SqlStatementSummary();
        foreach (var recognizer in recognizers) recognizer.Collect(entry, detected);
        void Add(bool present, string id)
        {
            var predicate = new SqlPredicateSummary(id, signature);
            if (present && !query.Predicates.Contains(predicate)) query.Predicates.Add(predicate);
        }
        Add(detected.FunctionOnColumnOccurrences.Count > 0, "FUNCTION_ON_COLUMN");
        Add(detected.LikeOccurrences.Any(o => o.HasLeadingWildcard), "LEADING_WILDCARD_SEARCH");
        Add(detected.LikeOccurrences.Any(o => !o.HasWildcard), "LIKE_WITHOUT_WILDCARD");
        Add(detected.CastOnColumnOccurrences.Count > 0, "CAST_ON_COLUMN");
        Add(detected.ArithmeticOnColumnOccurrences.Count > 0, "ARITHMETIC_ON_COLUMN");
        Add(detected.YearExtractionOccurrences.Count > 0, "YEAR_EXTRACTION_IN_PREDICATE");
        Add(detected.ConcatenationOccurrences.Count > 0, "CONCATENATION_IN_PREDICATE");
        Add(detected.OptionalParameterFilters.Count > 0, "OPTIONAL_PARAMETER_FILTER");
        Add(detected.CaseInPredicateOccurrences.Count > 0, "CASE_IN_PREDICATE");
        Add(detected.RowFiltersInHaving.Count > 0, "ROW_FILTER_IN_HAVING");
    }
}
