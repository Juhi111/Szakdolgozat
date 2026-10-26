using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Collections;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    /// <summary>
    /// A Protobuf-mezőleírók segítségével, verem használatával járja be az AST-t.
    /// Minden csomóponthoz elkészíti a bejárási kontextust.
    /// </summary>
    public class PostgreSqlAstWalker
    {
        /// <summary>
        /// Mélységi sorrendben bejárja a gyökeret és leszármazottait.
        /// A megadott műveletet minden csomóponton a gyermekei előtt hívja meg.
        /// </summary>
        /// <param name="root">A bejárás gyökércsomópontja.</param>
        /// <param name="statementIndex">Az SQL-utasítás egytől induló sorszáma.</param>
        /// <param name="visit">Az egyes bejárási bejegyzéseken végrehajtandó művelet.</param>
        /// <exception cref="ArgumentNullException">A gyökér vagy a végrehajtandó művelet null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Az utasítás sorszáma kisebb egynél.</exception>
        public void Walk(IMessage root, int statementIndex, Action<AstVisitEntry> visit) { 

            if(visit == null || root == null)
                throw new ArgumentNullException(visit == null ? nameof(visit) : nameof(root));
            
            var context = new AstVisitContext(statementIndex, node: root);

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

        /// <summary>
        /// Összegyűjti a csomópont közvetlen, üzenettípusú gyermekeit Protobuf-mezőszám szerinti sorrendben.
        /// A gyermekek kontextusában rögzíti a szülőt, a mezőnevet és listák esetén az elem indexét.
        /// </summary>
        /// <param name="parent">A vizsgált szülő bejárási bejegyzése.</param>
        /// <returns>A közvetlen gyermekek bejárási bejegyzései, a listaelemek sorrendjét megtartva.</returns>
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
                                var childContext = new AstVisitContext(parent.Context.StatementIndex, parent.Context, field.Name, itemIndex, node: child);
                                children.Add(new AstVisitEntry(child, childContext));
                            }
                            itemIndex++;
                        }
                    }
                }
                else if(value is IMessage child){

                    var childContext = new AstVisitContext(parent.Context.StatementIndex, parent.Context, field.Name, node: child);
                    children.Add(new AstVisitEntry(child, childContext));

                }
            }
            return children;
        }
    }
}
