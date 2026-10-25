using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class NaturalJoinUsageRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.NaturalJoins)
            {
                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "NATURAL_JOIN_USAGE",
                    Title = "Implicit kapcsolási oszlopok NATURAL JOIN esetén",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        "A lekérdezés NATURAL JOIN kapcsolatot használ. " +
                        "A kapcsolási oszlopokat a két bemenet közös oszlopnevei határozzák meg, " +
                        "ezért egy sémamódosítás a lekérdezés szövegének változtatása nélkül " +
                        "is megváltoztathatja a kapcsolat jelentését.",
                    Recommendation =
                        "Mérlegeld a szándékolt kapcsolási oszlopok explicit megadását JOIN ... USING " +
                        "vagy JOIN ... ON formában. Őrizd meg a kapcsolat típusát, " +
                        "például a LEFT JOIN működését. Az ON és USING eredményoszlopai eltérhetnek, " +
                        "ezért a kiválasztott oszlopokat is ellenőrizd. " +
                        "Az elemző sémaadatok nélkül nem tudja meghatározni a közös oszlopokat, " +
                        "és nem végez automatikus átírást. Ez karbantarthatósági jelzés, " +
                        "önmagában nem jelent teljesítményproblémát.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}