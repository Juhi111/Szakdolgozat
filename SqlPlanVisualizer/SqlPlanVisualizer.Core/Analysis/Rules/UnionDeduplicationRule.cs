using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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
                    Title = "UNION duplikációeltávolítással",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        "A lekérdezés UNION műveletet használ ALL nélkül. " +
                        "Ez eltávolítja az azonos eredménysorokat, ami az összefűzésen felül " +
                        "további feldolgozást igényelhet.",
                    Recommendation =
                        "Ellenőrizd, hogy szükséges-e az eredménysorok egyedisége. " +
                        "Ha az ismétlődések megengedettek, vagy a teljes eredmény egyedisége " +
                        "bizonyított, mérlegeld a UNION ALL használatát. " +
                        "Az ágak közötti átfedés hiánya önmagában nem elég: " +
                        "az egyes ágakon belüli ismétlődések is számítanak. " +
                        "Összetett halmazműveletnél a teljes kifejezés jelentését ellenőrizd. " +
                        "Az elemző nem ismeri az egyediségi megszorításokat, ezért nem végez automatikus átírást. " +
                        "A tényleges költséget a végrehajtási terv alapján vizsgáld meg.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}