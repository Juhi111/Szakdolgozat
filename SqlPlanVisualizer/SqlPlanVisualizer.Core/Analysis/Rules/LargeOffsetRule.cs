using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "Nagy OFFSET érték",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        $"A lekérdezés {occurrence.RowOffset} sort hagy ki, " +
                        $"ami eléri a beállított {threshold} soros figyelmeztetési küszöböt. " +
                        "A kihagyott sorok előállítása és feldolgozása is munkát igényelhet, " +
                        "ezért a lapozás költsége az eltolással növekedhet.",
                    Recommendation =
                        "Ellenőrizd a végrehajtási tervet és a válaszidőt nagyobb eltolásoknál is. " +
                        "Ha egymást követő oldalakat kérsz le, mérlegeld a keyset lapozást: " +
                        "a következő oldalt az előző oldal utolsó rendezési kulcsértékei alapján keresd. " +
                        "Ehhez egyértelmű rendezés, szükség esetén egyedi holtversenykulcs, " +
                        "valamint megfelelő NULL-kezelés szükséges. " +
                        "A megoldás nem általános helyettesítő tetszőleges oldalszámra ugráshoz. " +
                        "A küszöb alkalmazásbeállítás, nem PostgreSQL-korlát; önmagában nem bizonyít lassulást.",
                    SourceOffset = occurrence.SourceOffset
                });
            }

            return findings;
        }
    }
}