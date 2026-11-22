using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

public sealed class DuplicateInRewriteRule : PredicateRewriteRule
{
    protected override string RuleId => "DUPLICATE_IN_REWRITE";
    protected override string Title => "Ismétlődő IN-listaelemek eltávolítása";
    protected override string Explanation => "Az IN/NOT IN listában az azonos literálokat egyszer tartottuk meg, az első előfordulás sorrendjében. Nem feltételezünk gyorsulást; a cél a felesleges ismétlés csökkentése.";
    protected override string Preconditions => "Csak homogén, nem NULL egész vagy szöveg literálok. A szövegeket pontosan, kis- és nagybetűk megkülönböztetésével hasonlítjuk; kolláció alapján nem vonunk össze eltérő szövegeket.";
    protected override Node? Rewrite(Node predicate)
    {
        if (!ListPredicatePattern.InList(predicate, out var e, out var values)) return null;
        var distinct = values.DistinctBy(RewriteAst.LiteralKey).ToList();
        if (distinct.Count == values.Count) return null;
        var result = predicate.Clone();
        result.AExpr.Rexpr.List.Items.Clear();
        result.AExpr.Rexpr.List.Items.Add(distinct.Select(n => n.Clone()));
        return result;
    }
}
