using System;
using System.Collections.Generic;
using System.Globalization;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.Rules
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
                    Title = "Csoportosítás után szűrünk egy csoportosító oszlopra",
                    Description = $"A HAVING részben a(z) {predicate} feltétel egy GROUP BY oszlopot vizsgál, nem egy összesített értéket. A feltétel " +
                        "önállóan vagy AND kapcsolaton keresztül szerepel, ezért érdemes megvizsgálni a csoportosítás előtti szűrés lehetőségét. " +
                        $"Hely: {occurrence.Location}.",
                    Recommendation = "Ellenőrizd, áthelyezhető-e ez a feltétel a WHERE részbe az eredmény megőrzésével. A meglévő WHERE feltételhez megfelelő " +
                        "zárójelezéssel és AND kapcsolattal illeszd. Az összesített értékek feltételei maradjanak a HAVING részben. Változó " +
                        "eredményű vagy mellékhatásos kifejezéseknél külön ellenőrzés kell. A PostgreSQL ezt az áthelyezést már magától is " +
                        "elvégezheti, ezért hasonlítsd össze a terveket.",
                    SourceOffset = occurrence.SourceOffset
                });
            }
            return findings;
        }
    }
}
