using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Collections;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class PostgreSqlAstWalker
    {
        public void Walk(IMessage root, int statementIndex, Action<AstVisitEntry> visit) { 

            if(visit == null || root == null)
                throw new ArgumentNullException(visit == null ? nameof(visit) : nameof(root));

            var context = new AstVisitContext(statementIndex);

            Stack<AstVisitEntry> stack = new Stack<AstVisitEntry>();

            stack.Push(new AstVisitEntry(root, context));

            while (stack.Count > 0) {

                var item = stack.Pop();
                visit(item);

                var children = GetChildren(item);
                for (int i = children.Count - 1; i >= 0; i--) {
                    stack.Push(children[i]);
                }

            }
        }

        private List<AstVisitEntry> GetChildren(AstVisitEntry parent) {

            List<AstVisitEntry> children = new();

            foreach (var field in parent.Node.Descriptor.Fields.InFieldNumberOrder()) {
                if (field.FieldType != FieldType.Message)
                    continue;

                var value = field.Accessor.GetValue(parent.Node);

                if (field.IsRepeated)
                {
                    if (value is IEnumerable items)
                    {
                        int itemIndex = 0;

                        foreach (var item in items)
                        {

                            if (item is IMessage child)
                            {

                                var childContext = new AstVisitContext(parent.Context.StatementIndex, parent.Context, field.Name, itemIndex);
                                children.Add(new AstVisitEntry(child, childContext));
                            }
                            itemIndex++;
                        }
                    }
                }
                else if(value is IMessage child){ 

                    var childContext = new AstVisitContext(parent.Context.StatementIndex, parent.Context, field.Name);
                    children.Add(new AstVisitEntry(child, childContext));

                }
            }
            return children;
        }
    }
}
