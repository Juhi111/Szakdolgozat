using PgSqlParser;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast
{
    /// <summary>
    /// Az AST-csomópontot tartalmazó SELECT-részt jelöli.
    /// Az Unknown érték azt jelenti, hogy a rész nem határozható meg.
    /// </summary>
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

    /// <summary>
    /// Az AST szülői lánca alapján meghatározza a legközelebbi SELECT-et
    /// és a csomópont lekérdezésen belüli helyét.
    /// </summary>
    public class AstContextResolver
    {
        /// <summary>
        /// Meghatározza, hogy a csomópont a legközelebbi SELECT melyik részében szerepel.
        /// A JOIN ON feltételt külön kezeli, és nem lépi át a legközelebbi SELECT határát.
        /// </summary>
        /// <param name="context">A vizsgált csomópont kontextusa.</param>
        /// <returns>A felismert lekérdezésrész, vagy Unknown, ha nem határozható meg.</returns>
        /// <exception cref="ArgumentNullException">A kontextus null.</exception>
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

