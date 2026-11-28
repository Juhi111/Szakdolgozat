using SqlPlanVisualizer.Core.Analysis;
using SqlPlanVisualizer.Core.Interfaces;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Database;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql;

/// <summary>A javaslatot az eredeti tervhez tartozó kapcsolaton kéri le; az eredeti elemzést nem módosítja.</summary>
public sealed class SqlRewriteComparisonService(IDatabaseExplainService explainService, SqlAnalysisService sqlAnalysis, PlanAnalysisService planAnalysis, IDatabaseMetadataService? metadataService = null, IDatabaseIndexMetadataService? relationalMetadataService = null)
{
    public static void ValidateSource(CombinedAnalysisResult original, SqlRewriteSuggestion suggestion, DatabaseConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(suggestion);
        ArgumentNullException.ThrowIfNull(options);
        var origin = original.Pair.Plan.Origin;
        if (origin?.Source != PlanSourceKind.Database || origin.SqlText != suggestion.OriginalSql ||
            origin.StatementIndex != original.Pair.Statement.StatementIndex)
            throw new InvalidOperationException("Ehhez az összehasonlításhoz kérd le az eredeti SQL tervét az alkalmazásból, majd készíts új összegzést.");
        if (origin.Host != options.Host || origin.Port != options.Port || origin.Database != options.Database ||
            origin.Schema != options.Schema || origin.Username != options.Username)
            throw new InvalidOperationException("A kiválasztott kapcsolat eltér az eredeti terv kapcsolatától. Válaszd vissza az eredeti kapcsolatot.");
    }

    public async Task<RewriteMetadataCheck> CheckMetadataAsync(CombinedAnalysisResult original, SqlRewriteSuggestion suggestion,
        DatabaseConnectionOptions options, string password, CancellationToken cancellationToken = default)
    {
        ValidateSource(original, suggestion, options);
        cancellationToken.ThrowIfCancellationRequested();
        if (suggestion.AppliedSteps.Count > 0 || suggestion.RuleId == SqlRewriteSuggestion.CombinedRuleId)
            return await CheckCombinedAsync(suggestion, options.Copy(), password, cancellationToken);
        return await CheckSingleAsync(suggestion, options, password, cancellationToken);
    }

    private async Task<RewriteMetadataCheck> CheckCombinedAsync(SqlRewriteSuggestion suggestion, DatabaseConnectionOptions options,
        string password, CancellationToken token)
    {
        static RewriteMetadataCheck Invalid() => new(MetadataCheckStatus.Failed, "Az összevont javaslat lépései nem igazolhatók. Készíts új elemzést.");
        if (suggestion.AppliedSteps.Count is < 2 or > 256) return Invalid();
        string current = suggestion.OriginalSql;
        var checks = new List<RewriteMetadataCheck>();
        var rules = Rewriting.SqlRewriteRuleFactory.CreateDefault();
        foreach (var step in suggestion.AppliedSteps)
        {
            token.ThrowIfCancellationRequested();
            if (step.OriginalSql != current || step.AppliedSteps.Count != 0 || step.SuggestedSql == current) return Invalid();
            SqlRewriteSuggestion verified = step;
            // A kapcsolati szabályt a friss katalógusos ellenőrzés generálja újra.
            if (!Rewriting.RelationalRewriteRule.IsRelationalRule(step.RuleId))
            {
                try
                {
                    var match = rules.SelectMany(r => r.Suggest(current))
                        .FirstOrDefault(s => s.RuleId == step.RuleId && s.SuggestedSql == step.SuggestedSql);
                    if (match is null) return Invalid();
                    verified = match; // A tárolt metaadatfeltétel helyett az újra feloldott oszlopot ellenőrizzük.
                }
                catch (FormatException) { return Invalid(); }
            }
            var check = await CheckSingleAsync(verified, options, password, token);
            checks.Add(check);
            if (check.BlocksExecution)
                return check with { Message = $"A(z) {checks.Count}. módosítás nem alkalmazható ({step.Title}). {check.Message}" };
            current = verified.SuggestedSql;
        }
        if (current != suggestion.SuggestedSql) return Invalid();
        bool allPassed = checks.All(c => c.Status == MetadataCheckStatus.Passed);
        return new(allPassed ? MetadataCheckStatus.Passed : MetadataCheckStatus.Unknown,
            $"Az összevont javaslat {checks.Count} lépését ellenőriztük. " +
            (allPassed ? "A vizsgált metaadatfeltételek teljesülnek." : "Egyes előfeltételek kézi ellenőrzést igényelnek.") +
            "\n" + string.Join("\n", checks.Select((c, i) => $"{i + 1}. {c.Description}")),
            CheckedAt: DateTimeOffset.UtcNow, Source: $"{options.Host}:{options.Port}/{options.Database}");
    }

    private async Task<RewriteMetadataCheck> CheckSingleAsync(SqlRewriteSuggestion suggestion, DatabaseConnectionOptions options,
        string password, CancellationToken cancellationToken)
    {
        if (suggestion.RequiresRelationalMetadata || Rewriting.RelationalRewriteRule.IsRelationalRule(suggestion.RuleId))
            return await RelationalRewritePreflight.CheckAsync(suggestion, options.Copy(), password, relationalMetadataService, cancellationToken);
        var requirement = suggestion.MetadataRequirement;
        if (requirement is null) return RewriteMetadataCheck.NotChecked("Ehhez a javaslathoz nincs feloldható, támogatott típusfeltétel.");
        if (metadataService is null) return RewriteMetadataCheck.NotChecked("A metaadat-szolgáltatás nem érhető el.");
        var snapshot = options.Copy();
        try
        {
            var column = await metadataService.GetColumnAsync(snapshot, password, requirement.Column, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return RewriteMetadataEvaluator.Evaluate(requirement.Type, column) with
            {
                CheckedAt = metadataService is AnalysisMetadataSnapshotReader cached ? cached.CapturedAt : DateTimeOffset.UtcNow,
                Source = $"{snapshot.Host}:{snapshot.Port}/{snapshot.Database}; séma: {snapshot.Schema}; felhasználó: {snapshot.Username}"
            };
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or TimeoutException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RewriteMetadataCheck.NotChecked("A metaadat-lekérés nem sikerült. Ellenőrizd a kapcsolatot és a jogosultságokat; a típusfeltétel ismeretlen maradt.");
        }
    }

    public async Task<SqlRewriteComparison> CompareAsync(CombinedAnalysisResult original, SqlRewriteSuggestion suggestion,
        DatabaseConnectionOptions options, string password, bool analyze, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(password);
        var snapshot = options.Copy();
        ValidateSource(original, suggestion, snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        var metadataCheck = await CheckMetadataAsync(original, suggestion, snapshot, password, cancellationToken);
        if (metadataCheck.BlocksExecution) throw new RewriteMetadataException(metadataCheck);
        var summary = new PostgreSqlQueryService().ParseSummary(suggestion.SuggestedSql);
        if (!summary.IsSuccess || summary.Statements.Count != 1 || summary.Statements[0].StatementType != "SELECT")
            throw new InvalidOperationException("A javaslatnak egyetlen érvényes SELECT utasítást kell tartalmaznia.");
        var origin = new PlanOrigin
        {
            Source = PlanSourceKind.Database, Mode = analyze ? PlanCaptureMode.Actual : PlanCaptureMode.Estimated,
            CapturedAt = DateTimeOffset.UtcNow, SqlText = suggestion.SuggestedSql,
            DocumentHash = PlanOriginMatcher.HashDocument(suggestion.SuggestedSql), StatementIndex = 1,
            Host = snapshot.Host, Port = snapshot.Port, Database = snapshot.Database, Schema = snapshot.Schema, Username = snapshot.Username
        };
        string json = analyze
            ? await explainService.GetActualPlanAsync(snapshot, password, suggestion.SuggestedSql, cancellationToken)
            : await explainService.GetEstimatedPlanAsync(snapshot, password, suggestion.SuggestedSql, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var plans = new PostgreSqlPlanService().ParsePlans(json, origin);
        if (plans.Count != 1)
            throw new InvalidOperationException("A javaslat lekérése nem pontosan egy tervet adott vissza.");
        var sqlFindings = sqlAnalysis.Analyze(summary.Statements);
        var planFindings = planAnalysis.Analyze(new[] { plans[0].Root });
        return new SqlRewriteComparison(original, suggestion, plans[0], sqlFindings, planFindings) { MetadataCheck = metadataCheck };
    }
}

public sealed class RewriteMetadataException(RewriteMetadataCheck check) : InvalidOperationException(check.Description)
{
    public RewriteMetadataCheck Check { get; } = check;
}
