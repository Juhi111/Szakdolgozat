using PgSqlParser;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class HavingGroupKeyRewriteRule : ISqlRewriteRule
{
    public IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        var parsed = RewriteAst.Parse(sql);
        if (parsed.Stmts.Count != 1 || parsed.Stmts[0].Stmt.SelectStmt is null) return Array.Empty<SqlRewriteSuggestion>();
        var result = new List<SqlRewriteSuggestion>();
        var scopes = RewriteAst.QueryScopes(parsed);
        for (int scopeIndex = 0; scopeIndex < scopes.Count; scopeIndex++)
        {
            var select = scopes[scopeIndex];
            if (!RewriteAst.Supported(select) || select.GroupClause.Count == 0 || select.GroupClause.Any(n => !RewriteAst.Column(n)) ||
                RewriteAst.Contains(select, n => n is SubLink || n is FuncCall f && RewriteAst.Name(f) is not ("count" or "sum" or "min" or "max" or "avg")))
                continue;
            var predicates = RewriteAst.Slots(select, having: true);
            for (int i = 0; i < predicates.Count; i++)
            {
                var e = predicates[i].Node.AExpr;
                if (RewriteAst.Op(e) is not ("=" or "<>" or "<" or "<=" or ">" or ">=")) continue;
                Node? column = null;
                if (RewriteAst.Column(e!.Lexpr) && RewriteAst.Integer(e.Rexpr, out _)) column = e.Lexpr;
                else if (RewriteAst.Column(e.Rexpr) && RewriteAst.Integer(e.Lexpr, out _)) column = e.Rexpr;
                if (column is null || !select.GroupClause.Any(key => RewriteAst.SameColumn(key, column))) continue;
                var clone = parsed.Clone();
                var query = RewriteAst.QueryScopes(clone)[scopeIndex];
                var slots = RewriteAst.Slots(query, having: true);
                var moved = slots[i].Node.Clone();
                var remaining = slots.Where((_, index) => index != i).Select(s => s.Node.Clone()).ToList();
                query.HavingClause = remaining.Count == 0 ? null : RewriteAst.And(remaining);
                query.WhereClause = query.WhereClause is null ? moved : RewriteAst.And(new[] { query.WhereClause, moved });
                var deparsed = Parser.Deparse(clone);
                if (deparsed.Error is not null || string.IsNullOrWhiteSpace(deparsed.Value)) continue;
                RewriteAst.Parse(deparsed.Value);
                result.Add(new SqlRewriteSuggestion
                {
                    RuleId = "HAVING_GROUP_KEY_REWRITE", Title = "Csoportosító oszlop szűrése a csoportosítás előtt",
                    OriginalSql = sql, SuggestedSql = deparsed.Value,
                    Explanation = "Egy csoportosító oszlop egész literállal végzett összehasonlítását áthelyeztük a WHERE részbe. " +
                        "A meglévő WHERE zárójelezett AND kapcsolatot kap, a többi HAVING feltétel megmarad. Az optimalizáló ezt már magától is elvégezheti; gyorsulást nem ígérünk. A formázás változhat, a megjegyzések kimaradnak.",
                    Preconditions = "Csak egyszerű, közvetlen oszlopok szerinti GROUP BY és önálló/AND alatti HAVING. " +
                        "ROLLUP/CUBE/GROUPING SETS és összetett OR nem támogatott. Beépített típusokat és tiszta aggregátumokat feltételezünk. " +
                        "Az aggregálás kihagyása nem fedhet el túlcsordulást vagy más hibát. Az eredményazonosságot és a teljesítményt külön ellenőrizd."
                });
            }
        }
        return result;
    }
}
