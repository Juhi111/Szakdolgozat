using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
{
    public class FunctionOnColumnRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.FunctionOnColumnOccurrences)
            {
                string columnReference =
                    string.IsNullOrEmpty(occurrence.Qualifier)
                        ? occurrence.ColumnName
                        : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                string expression =
                    $"{occurrence.FunctionName}({columnReference})";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,

                    RuleId = "FUNCTION_ON_COLUMN",

                    Title = "Szűrés függvénnyel átalakított oszlopra",

                    Description = $"A szűrés a(z) {expression} kifejezést használja az oszlop eredeti értéke helyett. Az eredeti oszlopra készített " +
                        $"hagyományos index nem feltétlenül alkalmas ennek a keresésnek a gyorsítására. Hely: {occurrence.Location}.",

                    Recommendation = "Gyakori keresésnél ellenőrizd, van-e a teljes keresési kifejezéshez illeszkedő index, vagy érdemes-e ilyet létrehozni. " +
                        "Ezt nevezzük kifejezésindexnek. Összetett feltételnél a belső függvényhívás indexelése önmagában kevés lehet. A " +
                        "függvény elhagyása megváltoztathatja az egyezés szabályait, ezért csak azonos jelentésű feltételre cserélj. A várható " +
                        "előnyt a végrehajtási tervvel ellenőrizd.",

                    SourceOffset = occurrence.SourceOffset
                });
            }

            return findings;
        }
    }
}
