using System;
using System.Collections.Generic;
using System.Globalization;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class ArithmeticOnColumnRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.ArithmeticOnColumnOccurrences)
            {
                string columnReference =
                    string.IsNullOrEmpty(occurrence.Qualifier)
                        ? occurrence.ColumnName
                        : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                string constant = occurrence.ConstantValue.ToString(
                    CultureInfo.InvariantCulture);

                string expression = occurrence.IsColumnOnLeft
                    ? $"{columnReference} {occurrence.Operator} {constant}"
                    : $"{constant} {occurrence.Operator} {columnReference}";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,

                    RuleId = "ARITHMETIC_ON_COLUMN",

                    Title = "Aritmetikai művelet oszlopon szűrési feltételben",

                    Description =
                        $"Hely: {occurrence.Location}. "
                        + $"A(z) {expression} kifejezés WHERE vagy "
                        + "JOIN ON környezetben szerepel. "
                        + "A feltétel az oszlopból számított értéket használja, "
                        + "ami befolyásolhatja az eredeti oszlop indexének "
                        + "felhasználhatóságát. "
                        + "Ez önmagában nem bizonyít lassulást.",

                    Recommendation =
                        "Ellenőrizd a végrehajtási tervet "
                        + "EXPLAIN (ANALYZE, BUFFERS) segítségével. "
                        + "Vizsgáld meg, hogy a feltétel átírható-e úgy, "
                        + "hogy az oszlop önmagában szerepeljen az "
                        + "összehasonlítás egyik oldalán. "
                        + "Az átrendezésnél figyelembe kell venni az "
                        + "adattípusokat, a túlcsordulást, a kerekítést, "
                        + "az egészosztást és a nullával osztást. "
                        + "Negatív számmal történő szorzás vagy osztás "
                        + "az egyenlőtlenség irányát is megfordíthatja. "
                        + "Automatikus átírás helyett ellenőrizd "
                        + "az eredmények és a hibaviselkedés megmaradását. "
                        + "Ha a számított kifejezés szükséges és gyakori, "
                        + "vizsgáld meg a teljes keresési kifejezéshez "
                        + "illeszkedő kifejezésindex lehetőségét.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}