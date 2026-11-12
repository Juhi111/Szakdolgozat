namespace SqlPlanVisualizer.Core.Models
{
    /// <summary>
    /// Egy lekérdezési blokk szerkezeti jellemzőit tartalmazza.
    /// </summary>
    public class SqlQueryBlockSummary
    {
        public bool IsSetOperation { get; init; }
        public bool HasOrderBy { get; init; }
        public bool HasRowLimit { get; init; }
        public bool HasGroupBy { get; init; }
        public bool HasDistinct { get; init; }
        public bool IsUnionDistinct { get; init; }
        public bool OrdersByRandom { get; init; }
        public int? OffsetSourceOffset { get; init; }

        /// <summary>A felső szintű WHERE/HAVING felismert mintái, helyfüggetlen AST-azonosítóval.</summary>
        public List<SqlPredicateSummary> Predicates { get; } = new();

        /// <summary>
        /// Jelzi, hogy a FROM rész kizárólag közvetlen táblahivatkozásokat tartalmaz.
        /// </summary>
        public bool HasOnlyDirectRelations { get; init; }

        /// <summary>
        /// A FROM rész közvetlen táblahivatkozásai.
        /// </summary>
        public List<SqlRelationSummary> Relations { get; } = new();

    }
}