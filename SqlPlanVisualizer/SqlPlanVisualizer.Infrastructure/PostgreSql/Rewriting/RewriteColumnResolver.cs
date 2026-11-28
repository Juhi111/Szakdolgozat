using PgSqlParser;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>A módosított feltétel oszlopát oldja fel; azonos nevű másik feltételt nem használ helyette.</summary>
public static class RewriteColumnResolver
{
    public static bool RequiresTypeCheck(string ruleId) => ruleId is "DATE_CAST_RANGE_REWRITE" or "YEAR_RANGE_REWRITE" or "ARITHMETIC_EQUALITY_REWRITE" or "LIKE_LITERAL_REWRITE" or "BOOLEAN_CASE_REWRITE";

    public static RewriteMetadataRequirement? Resolve(string ruleId, SelectStmt select, Node predicate)
    {
        RewriteTypeRequirement? type = ruleId switch
        {
            "DATE_CAST_RANGE_REWRITE" or "YEAR_RANGE_REWRITE" => RewriteTypeRequirement.DateTimestampOrTimestampTz,
            "ARITHMETIC_EQUALITY_REWRITE" => RewriteTypeRequirement.IntegerOrNumeric,
            "LIKE_LITERAL_REWRITE" => RewriteTypeRequirement.Text,
            "BOOLEAN_CASE_REWRITE" => RewriteTypeRequirement.Boolean,
            _ => null
        };
        var relations = RewriteAst.DirectRelations(select);
        if (type is null || relations is null) return null;
        var columns = new List<ColumnRef>();
        RewriteAst.Contains(predicate, n => { if (n is ColumnRef column) columns.Add(column); return false; });
        if (columns.Count != 1 || columns[0].Fields.Any(f => f.String is null)) return null;
        var parts = columns[0].Fields.Select(f => f.String.Sval).ToArray();
        var matches = relations.Where(relation =>
        {
            if (!string.IsNullOrEmpty(relation.Catalogname)) return false;
            return parts.Length == 1 && relations.Count == 1 ||
                parts.Length == 2 && parts[0] == (relation.Alias?.Aliasname ?? relation.Relname) ||
                parts.Length == 3 && relation.Alias is null && parts[0] == relation.Schemaname && parts[1] == relation.Relname;
        }).ToArray();
        if (matches.Length != 1) return null;
        var relation = matches[0];
        string? schema = string.IsNullOrEmpty(relation.Schemaname) ? null : relation.Schemaname;
        return new(new(schema, relation.Relname, parts[^1]), type.Value);
    }
}
