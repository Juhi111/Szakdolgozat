using PgSqlParser;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>A módosított feltétel oszlopát oldja fel; azonos nevű másik feltételt nem használ helyette.</summary>
public static class RewriteColumnResolver
{
    public static RewriteMetadataRequirement? Resolve(string ruleId, SelectStmt select, Node predicate)
    {
        RewriteTypeRequirement? type = ruleId switch
        {
            "DATE_CAST_RANGE_REWRITE" or "YEAR_RANGE_REWRITE" => RewriteTypeRequirement.DateOrTimestamp,
            "ARITHMETIC_EQUALITY_REWRITE" => RewriteTypeRequirement.Integer,
            "LIKE_LITERAL_REWRITE" => RewriteTypeRequirement.Text,
            _ => null
        };
        if (type is null || !RewriteAst.Supported(select)) return null;
        var columns = new List<ColumnRef>();
        RewriteAst.Contains(predicate, n => { if (n is ColumnRef column) columns.Add(column); return false; });
        if (columns.Count != 1 || columns[0].Fields.Any(f => f.String is null)) return null;
        var parts = columns[0].Fields.Select(f => f.String.Sval).ToArray();
        var relation = select.FromClause[0].RangeVar;
        if (!string.IsNullOrEmpty(relation.Catalogname)) return null;
        string? schema = string.IsNullOrEmpty(relation.Schemaname) ? null : relation.Schemaname;
        bool matches = parts.Length == 1 || parts.Length == 2 && parts[0] == (relation.Alias?.Aliasname ?? relation.Relname) ||
            parts.Length == 3 && relation.Alias is null && schema is not null && parts[0] == schema && parts[1] == relation.Relname;
        return matches ? new(new(schema, relation.Relname, parts[^1]), type.Value) : null;
    }
}
