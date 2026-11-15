using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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
                    Title = "NULL szerepel a kizárandó értékek között",
                    Description = "A NOT IN listában közvetlenül NULL szerepel. Szokásos skalár összehasonlításnál egyezéskor hamis, egyezés hiányában " +
                        "ismeretlen az eredmény: önálló WHERE feltételként így egyetlen sort sem enged át. Összetett feltételnél a többi logikai " +
                        $"ág is számít. Hely: {occurrence.Location}.",
                    Recommendation = "Ha konkrét értékeket szeretnél kizárni, távolítsd el a véletlenül bekerült NULL-elemet. A hiányzó értékeket külön IS " +
                        "NULL vagy IS NOT NULL feltétellel kezeld. Ha a bal oldali NULL-értékeket meg szeretnéd tartani, ezt külön IS NULL ággal " +
                        "fejezd ki, és ellenőrizd a teljes feltétel zárójelezését.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
