namespace SqlPlanVisualizer.Core.Models
{
    public class ArithmeticOnColumnOccurrence
    {
        public string ColumnName { get; set; } = string.Empty;

        public string? Qualifier { get; set; }

        public string Operator { get; set; } = string.Empty;

        public long ConstantValue { get; set; }

        public bool IsColumnOnLeft { get; set; }

        public string Location { get; set; } = string.Empty;

        public int? SourceOffset { get; set; }
    }
}