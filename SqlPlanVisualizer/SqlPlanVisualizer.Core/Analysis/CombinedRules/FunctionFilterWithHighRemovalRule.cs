using System;
using System.Collections.Generic;
using System.Linq;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules
{
    public class FunctionFilterWithHighRemovalRule : ICombinedAnalysisRule
    {
        public IEnumerable<CombinedAnalysisFinding> Analyze(CombinedAnalysisResult analysis)
        {
            ArgumentNullException.ThrowIfNull(analysis);

            var query = analysis.Pair.Statement.TopLevelQuery;
            var node = analysis.Pair.Plan.Root;

            if (query is null || query.IsSetOperation || !query.HasOnlyDirectRelations ||
                query.Relations.Count != 1 || query.FunctionsInWhere.Count == 0)
            {
                return Array.Empty<CombinedAnalysisFinding>();
            }

            if (node.NodeType is not ("Seq Scan" or "Index Scan" or "Index Only Scan" or "Bitmap Heap Scan"))
                return Array.Empty<CombinedAnalysisFinding>();

            var relation = query.Relations[0];

            if (!PlanFilterMatcher.MatchesRelation(node, relation))
                return Array.Empty<CombinedAnalysisFinding>();

            var planFinding = analysis.PlanFindings.FirstOrDefault(finding =>
                finding.RuleId == "HIGH_FILTER_REMOVAL" && ReferenceEquals(finding.Node, node));

            var filter = PlanFilterMatcher.ReadText(node, "Filter");

            if (planFinding is null || filter is null)
                return Array.Empty<CombinedAnalysisFinding>();

            foreach (var occurrence in query.FunctionsInWhere)
            {
                if (!PlanFilterMatcher.MatchesQualifier(occurrence.Qualifier, relation))
                    continue;

                if (occurrence.FunctionName is not ("lower" or "upper"))
                    continue;

                if (!PlanFilterMatcher.ContainsFunction(filter, occurrence.FunctionName, occurrence.ColumnName, relation))
                    continue;

                return new[]
                {
                    new CombinedAnalysisFinding
                    {
                        RuleId = "FUNCTION_FILTER_WITH_HIGH_REMOVAL",
                        Title = "Függvényes szűrés sok eldobott sorral",
                        SqlObservation = $"A WHERE feltételben {occurrence.FunctionName} függvény szerepel " +
                            $"a(z) {relation.RelationName} tábla {occurrence.ColumnName} oszlopán.",
                        PlanObservation = planFinding.Description,
                        Description = "A párosított terv ugyanazon táblát olvasó művelete sok sort szűrt ki, " +
                            "és a Filter mezőjében a felismert függvényes kifejezés is szerepel. " +
                            "Több feltétel esetén az eldobott sorok száma a teljes szűrésre vonatkozik; " +
                            "ebből nem állapítható meg a függvény önálló hatása.",
                        Recommendation = "Vizsgáld meg a teljes szűrés szelektivitását és a művelet futási idejét. " +
                            "Gyakran futtatott lekérdezésnél ellenőrizd, hogy a függvényes kifejezéshez " +
                            "illeszkedő kifejezésindex segíthet-e. Az index szükségességét és hatását " +
                            "a teljes lekérdezéssel ellenőrizd.",
                        RelatedPlanFinding = planFinding
                    }
                };
            }

            return Array.Empty<CombinedAnalysisFinding>();
        }
    }
}