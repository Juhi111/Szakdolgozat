using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Interfaces
{
    /// <summary>Egyetlen SQL-utasításhoz önálló, egymástól független átírási javaslatokat készít.</summary>
    public interface ISqlRewriteRule
    {
        IReadOnlyList<SqlRewriteSuggestion> Suggest(string sql);
    }
}
