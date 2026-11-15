using System;
using System.Collections.Generic;
using System.Globalization;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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

                    Title = "Szűrés számított oszlopértékre",

                    Description = $"A szűrés az oszlop eredeti értéke helyett a(z) {expression} kifejezést vizsgálja. Emiatt az oszlop hagyományos indexe " +
                        $"nem feltétlenül tudja gyorsítani a keresést. Hely: {occurrence.Location}.",

                    Recommendation = "Vizsgáld meg, átírható-e a feltétel úgy, hogy az oszlop önmagában szerepeljen az összehasonlításban. Az egészosztás, " +
                        "kerekítés, túlcsordulás és nullával osztás miatt az átrendezés megváltoztathatja az eredményt vagy a hibaviselkedést; " +
                        "negatív szorzó vagy osztó az egyenlőtlenség irányát is megfordíthatja. Ha a számítás szükséges, gyakori keresésnél " +
                        "vizsgáld meg a teljes kifejezésre készített index lehetőségét. A változtatás hatását végrehajtási tervvel ellenőrizd.",

                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
