using System;
using System.Collections.Generic;
using System.Globalization;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis
{
    public class RowFilterInHavingRule : ISqlAnalysisRule
    {
        public List<SqlAnalysisFinding> Analyze(SqlStatementSummary statement)
        {
            if (statement is null)
                throw new ArgumentNullException(nameof(statement));

            var findings = new List<SqlAnalysisFinding>();

            foreach (var occurrence in statement.RowFiltersInHaving)
            {
                string columnReference = string.IsNullOrEmpty(occurrence.Qualifier)
                    ? occurrence.ColumnName
                    : $"{occurrence.Qualifier}.{occurrence.ColumnName}";

                string constant = occurrence.ConstantValue.ToString(CultureInfo.InvariantCulture);

                string predicate = occurrence.IsColumnOnLeft
                    ? $"{columnReference} {occurrence.Operator} {constant}"
                    : $"{constant} {occurrence.Operator} {columnReference}";

                findings.Add(new SqlAnalysisFinding
                {
                    StatementIndex = statement.StatementIndex,
                    RuleId = "ROW_FILTER_IN_HAVING",
                    Title = "Csoportosítási kulcs szűrése HAVING-ban",
                    Description =
                        $"Hely: {occurrence.Location}. " +
                        $"A(z) {predicate} feltétel a GROUP BY listában szereplő oszlophivatkozást szűri, " +
                        "és nem tartalmaz aggregáló függvényt. " +
                        "A feltétel közvetlenül a HAVING része, vagy annak AND kapcsolattal összekötött ága.",
                    Recommendation =
                        "Mérlegeld ennek a feltételnek a WHERE részbe helyezését, " +
                        "hogy a lekérdezés egyértelműen a csoportosítás előtti sorokat szűrje. " +
                        "Ha már van WHERE feltétel, megfelelő zárójelezéssel, AND kapcsolattal egészítsd ki. " +
                        "Az aggregált értékekre vonatkozó feltételeket hagyd a HAVING részben. " +
                        "Az átírás előtt ellenőrizd a teljes lekérdezés jelentését, különösen az illékony " +
                        "vagy mellékhatásos kifejezéseket. A PostgreSQL tervezője ezt a szűrést " +
                        "már magától is előrehozhatja, ezért gyorsulás nem garantált. " +
                        "A végrehajtási terveket EXPLAIN (ANALYZE, BUFFERS) segítségével hasonlítsd össze.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}