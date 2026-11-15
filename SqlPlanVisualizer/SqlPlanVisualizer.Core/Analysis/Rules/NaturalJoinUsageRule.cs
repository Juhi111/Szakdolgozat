using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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
                    Title = "A kapcsolási oszlopokat a NATURAL JOIN választja ki",
                    Description = "A NATURAL JOIN minden közös nevű oszlopot felhasznál a kapcsoláshoz. Egy új, azonos nevű oszlop ezért az SQL módosítása " +
                        $"nélkül is megváltoztathatja az eredményt. Ez elsősorban karbantarthatósági kockázat. Hely: {occurrence.Location}.",
                    Recommendation = "Add meg a szándékolt kapcsolási oszlopokat JOIN ... USING vagy JOIN ... ON formában. Tartsd meg a kapcsolás típusát, " +
                        "például a LEFT JOIN viselkedését. Az ON és USING eltérő eredményoszlopokat adhat, ezért az oszloplistát is ellenőrizd. " +
                        "Az elemző a táblák sémája nélkül nem tudja kiválasztani helyetted a kapcsolási kulcsokat.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
