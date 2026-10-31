using Google.Protobuf;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    /// <summary>
    /// Egy bejárt AST-csomópont helyét, típusát és szülői kapcsolatát tárolja.
    /// </summary>
    public class AstVisitContext
    {
        /// <summary>
        /// Létrehozza a csomópont kontextusát, és meghatározza a bejárási mélységét.
        /// </summary>
        /// <param name="statementIndex">Az SQL-utasítás egytől induló sorszáma.</param>
        /// <param name="parent">A szülő kontextusa; gyökér esetén null.</param>
        /// <param name="fieldName">A szülő azon mezőjének neve, amely a csomópontot tartalmazza.</param>
        /// <param name="itemIndex">A csomópont nullától induló listaindexe, ha listában szerepel.</param>
        /// <param name="nodeType">A csomópont típusa, ha nincs megadva csomópontpéldány.</param>
        /// <param name="node">A csomópont példánya; ha megadott, annak típusa az irányadó.</param>
        /// <exception cref="ArgumentOutOfRangeException">Az utasítás sorszáma kisebb egynél.</exception>
        public AstVisitContext(int statementIndex, AstVisitContext? parent = null, string? fieldName = null, int? itemIndex = null, Type? nodeType = null,
            IMessage? node = null)
        {
            if (statementIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(statementIndex), "StatementIndex must be greater than 0.");
            }

            StatementIndex = statementIndex;
            Parent = parent;
            FieldName = fieldName;
            ItemIndex = itemIndex;
            Node = node;
            NodeType = node?.GetType() ?? nodeType;
            Depth = parent is null ? 0 : parent.Depth + 1;
        }

        /// <summary>
        /// Az SQL-utasítás egytől induló sorszáma.
        /// </summary>
        public int StatementIndex { get; }

        /// <summary>
        /// A szülő kontextusa; a bejárás gyökerénél null.
        /// </summary>
        public AstVisitContext? Parent { get; }

        /// <summary>
        /// A csomópontot tartalmazó szülői mező neve, ha ismert.
        /// </summary>
        public string? FieldName { get; }

        /// <summary>
        /// A csomópont nullától induló listaindexe; listaindex hiányában null.
        /// </summary>
        public int? ItemIndex { get; }

        /// <summary>
        /// A csomópont .NET-típusa, ha ismert.
        /// </summary>
        public Type? NodeType { get; }

        /// <summary>
        /// A csomópont mélysége a bejárásban; a gyökér mélysége nulla.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// A kontextushoz tartozó Protobuf-csomópont, ha megadták.
        /// </summary>
        public IMessage? Node { get; }
    }
}