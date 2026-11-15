using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class UnionDeduplicationRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.UnionDeduplications)
            {
                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "UNION_DEDUPLICATION",
                    Title = "A UNION az ismétlődő sorokat is eltávolítja",
                    Description = $"A UNION az eredmények egyesítése mellett az azonos sorokat is eltávolítja. Ez további feldolgozást igényelhet a UNION ALL művelethez képest. Hely: {occurrence.Location}.",
                    Recommendation = "Ha az ismétlődések megengedettek, vagy a teljes eredmény biztosan egyedi, mérlegeld a UNION ALL használatát. Az ágak " +
                        "közötti átfedés hiánya nem elég: egy ágon belül is lehetnek ismétlődések. Összetett halmazműveletnél a teljes eredményt " +
                        "ellenőrizd. Az elemző nem ismeri az egyediségi megszorításokat; a módosítás helyességét és hatását külön vizsgáld meg.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
