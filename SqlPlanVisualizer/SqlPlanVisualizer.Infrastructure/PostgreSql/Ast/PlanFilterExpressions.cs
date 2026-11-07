using PgSqlParser;
using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;
using System.Text.Json;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

internal static class PlanFilterExpressions
{
    public static void Populate(PlanNode node)
    {
        if (!node.AdditionalProperties.TryGetValue("Filter", out var filter) ||
            filter.ValueKind != JsonValueKind.String) return;
        string? Text(string key) => node.AdditionalProperties.TryGetValue(key, out var v) &&
            v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        var relation = new SqlRelationSummary
        {
            RelationName = node.RelationName ?? string.Empty, Alias = Text("Alias"), SchemaName = Text("Schema")
        };
        try
        {
            var parsed = new PostgreSqlQueryParser().Parse("SELECT 1 WHERE " + filter.GetString());
            if (parsed.Stmts.Count != 1 || parsed.Stmts[0].Stmt.SelectStmt is not SelectStmt select) return;
            var resolver = new AstContextResolver();
            new PostgreSqlAstWalker().Walk(parsed.Stmts[0].Stmt, 1, entry =>
            {
                if (resolver.GetClause(entry.Context) != AstClause.Where ||
                    !ReferenceEquals(resolver.FindSelectContext(entry.Context)?.Node, select)) return;
                var signature = ExpressionFingerprint.Read(entry.Node, relation);
                if (signature is not null) node.FilterExpressionSignatures.Add(signature);
            });
        }
        catch (FormatException)
        {
            // EXPLAIN belső jelölése (például SubPlan 1) nem mindig önálló SQL.
            // Ilyenkor csak az összekapcsolás marad el; a terv továbbra is elemezhető.
        }
    }
}
