using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class NotInWithNullLiteralRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.NotInWithNullLiterals)
            {
                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "NOT_IN_WITH_NULL_LITERAL",
                    Title = "NULL-literál a NOT IN értéklistában",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        "A NOT IN értéklistája közvetlen NULL-literált tartalmaz. " +
                        "Szokásos skalár összehasonlításnál a listában szereplő értékkel egyező " +
                        "bal oldali értékre az eredmény hamis, egyezés hiányában pedig ismeretlen (NULL), " +
                        "nem igaz. Önálló WHERE feltételként ez nem engedi át a sorokat. " +
                        "Összetett logikai kifejezésben a teljes feltétel eredménye a többi ágtól is függ.",
                    Recommendation =
                        "Ellenőrizd, hogy a NULL véletlenül került-e az értéklistába. " +
                        "Ha csak konkrét értékeket szeretnél kizárni, távolítsd el a NULL-elemet. " +
                        "Ha a bal oldali NULL-értékeket is meg szeretnéd tartani, " +
                        "azt külön IS NULL feltétellel fejezd ki. " +
                        "A NOT IN listába írt NULL nem helyettesíti az IS NULL vagy IS NOT NULL vizsgálatot. " +
                        "Az átírásnál a kívánt NULL-kezelést és a teljes logikai feltételt is ellenőrizd.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}