namespace SqlPlanVisualizer.Core.Models.Occurrences;

public enum PredicateSimplificationKind { OrEqualityChain, SingletonIn, DuplicateInValues }

/// <summary>Egyszerűsíthető, literálokat összehasonlító szűrés helye.</summary>
public sealed class PredicateSimplificationOccurrence
{
    public PredicateSimplificationKind Kind { get; set; }
    public string Location { get; set; } = string.Empty;
    public int? SourceOffset { get; set; }
}
