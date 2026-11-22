using PgSqlParser;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>Egyetlen felső szintű WHERE-feltételt cserél egy klónozott AST-ben.</summary>
public abstract class PredicateRewriteRule : ISqlRewriteRule
{
    protected abstract string RuleId { get; }
    protected abstract string Title { get; }
    protected abstract string Explanation { get; }
    protected abstract string Preconditions { get; }
    protected abstract Node? Rewrite(Node predicate);

    public IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        var parsed = RewriteAst.Parse(sql);
        if (parsed.Stmts.Count != 1 || !RewriteAst.Supported(parsed.Stmts[0].Stmt.SelectStmt))
            return Array.Empty<SqlRewriteSuggestion>();
        var suggestions = new List<SqlRewriteSuggestion>();
        int count = RewriteAst.Slots(parsed.Stmts[0].Stmt.SelectStmt).Count;
        for (int i = 0; i < count; i++)
        {
            var candidate = parsed.Clone();
            var slot = RewriteAst.Slots(candidate.Stmts[0].Stmt.SelectStmt)[i];
            var replacement = Rewrite(slot.Node);
            if (replacement is null) continue;
            slot.Replace(replacement);
            var deparsed = Parser.Deparse(candidate);
            if (deparsed.Error is not null || string.IsNullOrWhiteSpace(deparsed.Value)) continue;
            RewriteAst.Parse(deparsed.Value);
            suggestions.Add(new SqlRewriteSuggestion
            {
                RuleId = RuleId, Title = Title, OriginalSql = sql, SuggestedSql = deparsed.Value,
                Explanation = Explanation + " Egy változat egy feltételt módosít. Az AST-ből generált SQL formázása változhat, megjegyzései kimaradnak.",
                Preconditions = Preconditions + " A beépített típusokat és operátorokat feltételezzük; a metaadatokat nem ellenőriztük. " +
                    "Az eredményazonosság és a gyorsulás nincs igazolva. Teszteld az eredményt és a tervet."
            });
        }
        return suggestions;
    }
}
