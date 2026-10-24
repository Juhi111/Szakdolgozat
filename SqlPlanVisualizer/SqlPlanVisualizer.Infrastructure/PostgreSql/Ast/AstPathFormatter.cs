namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    public class AstPathFormatter
    {
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
