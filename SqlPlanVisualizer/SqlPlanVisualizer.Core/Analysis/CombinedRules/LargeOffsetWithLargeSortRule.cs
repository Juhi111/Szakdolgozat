using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

public class LargeOffsetWithLargeSortRule : ICombinedAnalysisRule
{
    public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var query = analysis.Pair.Statement.TopLevelQuery;
        var root = analysis.Pair.Plan.Root;
        if (query is null || query.IsSetOperation || query.OffsetSourceOffset is not int offset || root.NodeType != "Limit")
            return Array.Empty<CombinedAnalysisFinding>();
        var sql = analysis.SqlFindings.FirstOrDefault(f => f.RuleId == "LARGE_OFFSET" && f.SourceOffset == offset);
        var plan = analysis.PlanFindings.FirstOrDefault(f => f.RuleId == "LARGE_SORT_FOR_LIMIT" && ReferenceEquals(f.Node, root));
        if (sql is null || plan is null) return Array.Empty<CombinedAnalysisFinding>();
        return new[]
        {
            new CombinedAnalysisFinding
            {
                RuleId = "LARGE_OFFSET_WITH_LARGE_SORT",
                RelatedSqlRuleId = "LARGE_OFFSET",
                Title = "A sok sort kihagyó lapozáshoz nagy rendezés tartozik",
                SqlObservation = sql.Description,
                PlanObservation = plan.Description,
                Description = "Az SQL fő lekérdezése nagy OFFSET értékkel lapoz, és a párosított terv ugyanazon Limit művelete alatt sok sor kerül a " +
                    "rendezésbe a visszaadott eredményhez képest. Érdemes együtt vizsgálni a lapozást és a rendezést. Ezekből az adatokból " +
                    "az OFFSET önálló időigénye nem különíthető el.",
                Recommendation = "Mérd meg a későbbi oldalak lekérését is. Egymás utáni lapozásnál vizsgáld meg az utolsó rendezési értékeken alapuló, " +
                    "egyértelmű sorrendű kulcsalapú lapozást. Ez nem közvetlen helyettesítő tetszőleges oldalra ugráshoz. Ellenőrizd, " +
                    "segíthet-e a szűrést és rendezést együtt kiszolgáló index, majd hasonlítsd össze az eredményt és a teljes futási időt.",
                RelatedPlanFinding = plan
            }
        };
    }
}
