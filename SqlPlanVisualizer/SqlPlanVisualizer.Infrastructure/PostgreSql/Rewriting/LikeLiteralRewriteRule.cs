using System.Text;
using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class LikeLiteralRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "LIKE_LITERAL_REWRITE";
    protected override string Title => "Wildcard nélküli LIKE egyszerűsítése";
    protected override string Explanation => "A LIKE/NOT LIKE nem tartalmazott aktív wildcardot. Az escape-karaktereket feloldva egyenlőségi vagy nemegyenlőségi összehasonlítást készítettünk. Ez elsősorban olvashatósági egyszerűsítés.";
    protected override string Preconditions => "Csak beépített text/varchar oszlopon, a LIKE és az összehasonlítás azonos kollációs viselkedése mellett használd. A char(n), citext és egyéni típusok nincsenek igazolva. ILIKE-ot nem alakítunk egyenlőséggé.";
    private static bool SupportedOperand(Node node) => RewriteAst.Column(node) ||
        node.FuncCall is { } f && RewriteAst.Name(f) is "lower" or "upper" && f.Args.Count == 1 &&
        RewriteAst.Column(f.Args[0]) && f.Over is null && f.AggFilter is null && !f.AggDistinct && f.AggOrder.Count == 0;

    protected override Node? Rewrite(Node predicate)
    {
        var e = predicate.AExpr;
        if (e?.Kind != A_Expr_Kind.AexprLike || e.Name.Count != 1 || !SupportedOperand(e.Lexpr)) return null;
        string? op = e.Name[0].String?.Sval;
        if (op is not ("~~" or "!~~")) return null;
        Node patternNode = e.Rexpr;
        string escape = "\\";
        if (patternNode?.FuncCall is { } f)
        {
            if (f.Funcname.Count != 2 || f.Funcname[0].String?.Sval != "pg_catalog" || RewriteAst.Name(f) != "like_escape" ||
                f.Args.Count != 2 || !RewriteAst.Text(f.Args[1], out escape)) return null;
            patternNode = f.Args[0];
        }
        if (!RewriteAst.Text(patternNode, out string pattern)) return null;
        var escapes = escape.EnumerateRunes().ToArray();
        if (escapes.Length > 1) return null;
        var runes = pattern.EnumerateRunes().ToArray();
        var literal = new StringBuilder();
        for (int i = 0; i < runes.Length; i++)
        {
            if (escapes.Length == 1 && runes[i] == escapes[0])
            {
                if (++i == runes.Length) return null;
                literal.Append(runes[i]);
            }
            else if (runes[i].Value is '%' or '_') return null;
            else literal.Append(runes[i]);
        }
        var constant = patternNode!.Clone();
        constant.AConst.Sval.Sval = literal.ToString();
        return RewriteAst.Binary(e.Lexpr, op == "~~" ? "=" : "<>", constant);
    }
}
