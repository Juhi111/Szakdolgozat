using Google.Protobuf;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    /// <summary>
    /// Egy bejárt AST-csomópontot és a hozzá tartozó kontextust fogja össze.
    /// </summary>
    public class AstVisitEntry
    {
        /// <summary>
        /// Létrehozza a bejárási bejegyzést a csomópontból és annak kontextusából.
        /// </summary>
        /// <param name="node">A bejárt Protobuf-csomópont.</param>
        /// <param name="context">A csomópont helyét és szülői kapcsolatát leíró kontextus.</param>
        /// <exception cref="ArgumentNullException">A csomópont vagy a kontextus null.</exception>
        public AstVisitEntry(IMessage node, AstVisitContext context)
        {
            if (node == null || context == null)
                throw new ArgumentNullException(node == null ? nameof(node) : nameof(context));

            Node = node;
            Context = context;
        }

        /// <summary>
        /// A bejárt Protobuf-csomópont.
        /// </summary>
        public IMessage Node { get; }

        /// <summary>
        /// A csomóponthoz tartozó bejárási kontextus.
        /// </summary>
        public AstVisitContext Context { get; }
    }
}