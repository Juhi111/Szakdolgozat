using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class LargeOffsetRule : ISqlAnalysisRule
    {
        private readonly long threshold;

        public LargeOffsetRule(long threshold = 10000)
        {
            if (threshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "A küszöbnek pozitívnak kell lennie.");

            this.threshold = threshold;
        }

        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.OffsetOccurrences)
            {
                if (occurrence.RowOffset < threshold)
                    continue;

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "LARGE_OFFSET",
                    Title = "Sok sort kihagyó lapozás",
                    Description = $"Az OFFSET {occurrence.RowOffset} sort hagy ki, ami eléri a beállított {threshold} soros jelzési küszöböt. A kihagyott " +
                        "sorokat is elő kellhet állítani, ezért a későbbi oldalak lekérése egyre több munkát igényelhet. A küszöb " +
                        $"alkalmazásbeállítás, nem PostgreSQL-korlát. Hely: {occurrence.Location}.",
                    Recommendation = "Mérd meg a későbbi oldalak válaszidejét is. Egymás utáni lapozásnál megfontolható a kulcsalapú (keyset) lapozás: a " +
                        "következő oldalt az előző oldal utolsó rendezési értékei alapján keresd. Ehhez egyértelmű sorrend kell, például dátum " +
                        "és egyedi azonosító együtt, valamint megfelelő NULL-kezelés. Ez nem közvetlen helyettesítő, ha tetszőleges oldalszámra " +
                        "kell ugrani.",
                    SourceOffset = occurrence.SourceOffset
                });
            }

            return findings;
        }
    }
}
