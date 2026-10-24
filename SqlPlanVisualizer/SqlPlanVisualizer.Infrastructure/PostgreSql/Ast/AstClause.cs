using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    public enum AstClause
    {
        Unknown,
        SelectList,
        From,
        JoinOn,
        Where,
        Having,
        GroupBy,
        OrderBy,
        DistinctOn,
        Limit,
        Offset,
        Window
    }
    public class AstContextResolver
    {
        public AstVisitContext? FindSelectContext(AstVisitContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            AstVisitContext? current = context;

            while (current != null)
            {
                if (current.NodeType == typeof(SelectStmt))
                    return current;

                current = current.Parent;
            }

            return null;
        }

        public AstClause GetClause(AstVisitContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            AstVisitContext? current = context;
            bool insideJoinOn = false;

            while (current != null)
            {                
                if (current.NodeType == typeof(SelectStmt))
                    return AstClause.Unknown;

                var parent = current.Parent;
                
                if (parent?.NodeType == typeof(JoinExpr) &&
                    current.FieldName == "quals")
                {
                    insideJoinOn = true;
                }
                
                if (parent?.NodeType == typeof(SelectStmt))
                {
                    if (insideJoinOn && current.FieldName == "from_clause")
                        return AstClause.JoinOn;

                    return current.FieldName switch
                    {
                        "target_list" => AstClause.SelectList,
                        "from_clause" => AstClause.From,
                        "where_clause" => AstClause.Where,
                        "having_clause" => AstClause.Having,
                        "group_clause" => AstClause.GroupBy,
                        "sort_clause" => AstClause.OrderBy,
                        "distinct_clause" => AstClause.DistinctOn,
                        "limit_count" => AstClause.Limit,
                        "limit_offset" => AstClause.Offset,
                        "window_clause" => AstClause.Window,
                        _ => AstClause.Unknown
                    };
                }
                current = parent;
            }
            return AstClause.Unknown;
        }
    }

}

