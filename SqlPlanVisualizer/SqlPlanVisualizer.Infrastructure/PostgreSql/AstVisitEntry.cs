using Google.Protobuf;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class AstVisitEntry
    {
        public AstVisitEntry(IMessage node, AstVisitContext context)
        {
            if(node == null || context == null)
                throw new ArgumentNullException(node == null ? nameof(node) : nameof(context));
            Node = node;
            Context = context;
        }

        public IMessage Node { get;}
        public AstVisitContext Context { get;}

    }
}
