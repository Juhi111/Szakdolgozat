using System;
using System.Collections.Generic;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
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

                    Title = "Függvény alkalmazása oszlopra szűrési feltételben",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A {expression} függvényhívás WHERE vagy "
                        + "JOIN ON környezetben szerepel. "
                        + "A feltétel az oszlop átalakított értékét használja, "
                        + "ezért az eredeti oszlopon lévő egyszerű index "
                        + "nem feltétlenül alkalmas a keresés gyorsítására. "
                        + "Ez önmagában nem bizonyít teljesítményproblémát.",

                    Recommendation =
                        "Ellenőrizd a végrehajtási tervet "
                        + "EXPLAIN (ANALYZE, BUFFERS) segítségével. "
                        + "Gyakran használt feltételnél vizsgáld meg "
                        + "a teljes keresési kifejezéshez illeszkedő "
                        + "kifejezésindex lehetőségét és a már meglévő "
                        + "indexeket. Összetett kifejezésnél a belső "
                        + "függvényhívásra készített index önmagában "
                        + "nem feltétlenül elegendő. "
                        + "A függvényt csak akkor hagyd el vagy írd át, "
                        + "ha az összehasonlítás jelentése megmarad.",

                    SourceOffset = occurrence.SourceOffset
                });
            }

            return findings;
        }
    }
}