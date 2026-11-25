namespace SqlPlanVisualizer.Core.Models;

/// <summary>Az eredeti elemzés és egy külön lekért, önállóan elemzett javaslat.</summary>
public sealed record SqlRewriteComparison(CombinedAnalysisResult Original, SqlRewriteSuggestion Suggestion,
    ExecutionPlan SuggestedPlan, IReadOnlyList<SqlAnalysisFinding> SqlFindings, IReadOnlyList<PlanAnalysisFinding> PlanFindings)
{
    public SqlPlanVisualizer.Core.Models.Database.RewriteMetadataCheck MetadataCheck { get; init; } = SqlPlanVisualizer.Core.Models.Database.RewriteMetadataCheck.NotChecked();
    /// <summary>Rövid felületi értékelés; a részletes mérési körülmények külön megmaradnak.</summary>
    public string TimingSummary
    {
        get
        {
            if (Original.Pair.Plan.Origin?.Mode != PlanCaptureMode.Actual || SuggestedPlan.Origin?.Mode != PlanCaptureMode.Actual)
                return "A futásidők összehasonlításához mindkét változathoz futtatás és mérés szükséges.";
            if (Original.ExecutionTime is not double before || SuggestedPlan.ExecutionTime is not double after ||
                !double.IsFinite(before) || !double.IsFinite(after) || before <= 0 || after <= 0)
                return "Nincs elegendő futásidőadat az összehasonlításhoz.";
            double ratio = Math.Max(before, after) / Math.Min(before, after);
            string result = Math.Round(ratio, 2) == 1 ? "A két változat futásideje közel azonos volt."
                : !double.IsFinite(ratio) ? (before > after ? "A javasolt változat rövidebb ideig futott." : "A javasolt változat hosszabb ideig futott.")
                : $"A javasolt változat {ratio:N2}× {(before > after ? "gyorsabb" : "lassabb")} volt.";
            return result + " Egyetlen mérési pár eredménye.";
        }
    }

    public string TimingAssessment
    {
        get
        {
            if (Original.Pair.Plan.Origin?.Mode != PlanCaptureMode.Actual || SuggestedPlan.Origin?.Mode != PlanCaptureMode.Actual)
                return "Futásiidő-arányt csak két mért tervből számítunk. A becsült költség nem milliszekundum.";
            if (Original.ExecutionTime is not double before || SuggestedPlan.ExecutionTime is not double after ||
                !double.IsFinite(before) || !double.IsFinite(after) || before <= 0 || after <= 0)
                return "Nincs két pozitív, érvényes futásiidő-adat az arány kiszámításához.";
            return $"Eredeti / javasolt futásidő: {before / after:N2}. Ez két külön időpontban végzett mérés; " +
                "a gyorsítótár, terhelés, adatok, indexek és szerverbeállítások eltérhettek. Nem bizonyít tartós gyorsulást.";
        }
    }
    public string EquivalenceStatus => "Az eredményazonosság nincs ellenőrizve. A sorok számának egyezése sem bizonyítja azt.";
}
