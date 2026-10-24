namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    public class AstVisitContext
    {
        public AstVisitContext(int statementIndex, AstVisitContext? parent = null, string? fieldName = null, int? itemIndex = null, Type? nodeType = null)
        {
            if(statementIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(statementIndex), "StatementIndex must be greater than 0.");
            StatementIndex = statementIndex;
            Parent = parent;
            FieldName = fieldName;
            ItemIndex = itemIndex;
            NodeType = nodeType;
            Depth = parent == null ? 0 : parent.Depth + 1;
        }

        public int StatementIndex { get;}
        public AstVisitContext? Parent { get; } 
        public string? FieldName { get;}
        public int? ItemIndex { get; }
        public Type? NodeType { get; }
        public int Depth { get;}        
    }
}
