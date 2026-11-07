using SqlPlanVisualizer.Core.Models.Occurences;

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

        /// <summary>
        /// Jelzi, hogy a FROM rész kizárólag közvetlen táblahivatkozásokat tartalmaz.
        /// </summary>
        public bool HasOnlyDirectRelations { get; init; }

        /// <summary>
        /// A FROM rész közvetlen táblahivatkozásai.
        /// </summary>
        public List<SqlRelationSummary> Relations { get; } = new();

        /// <summary>
        /// A lekérdezési blokk WHERE feltételében felismert oszlopon végzett függvényhívások.
        /// </summary>
        public List<FunctionOnColumnOccurrence> FunctionsInWhere { get; } = new();
    }
}