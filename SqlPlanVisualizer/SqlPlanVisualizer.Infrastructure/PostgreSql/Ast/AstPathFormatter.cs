namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    public class AstPathFormatter
    {
        /// <summary>
        /// A szülői lánc mezőneveiből és listaindexeiből összeállítja
        /// a csomópont technikai AST-útvonalát, a gyökértől a csomópont felé haladva.
        /// </summary>
        /// <param name="context">A csomópont kontextusa.</param>
        /// <returns>Az útvonalelemek „ / ” jellel elválasztva, vagy üres szöveg, ha nincs mezőnév.</returns>
        /// <exception cref="ArgumentNullException">A kontextus null.</exception>
        public string Format(AstVisitContext context) { 
            if(context == null)             
                throw new ArgumentNullException(nameof(context));

            Stack<string> pathSegments = new();
            AstVisitContext? current = context;

            while (current != null)
            {
                if (current.FieldName != null)
                {
                    string segment = current.ItemIndex.HasValue
                        ? $"{current.FieldName}[{current.ItemIndex.Value}]"
                        : current.FieldName;

                    pathSegments.Push(segment);
                }

                current = current.Parent;
            }
            return string.Join(" / ", pathSegments);
        }
    }
}
