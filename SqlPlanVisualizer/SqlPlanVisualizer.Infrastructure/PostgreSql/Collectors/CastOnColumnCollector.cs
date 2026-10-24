using System.Collections.Generic;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class CastOnColumnCollector : CollectorBase<TypeCast>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(TypeCast typeCast, AstVisitContext context, SqlStatementSummary summary)
        {            
            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where &&
                clause != AstClause.JoinOn)
            {
                return;
            }
            
            var column = typeCast.Arg?.ColumnRef;

            if (column is null)
                return;

            if (!AstColumnReferenceReader.TryRead(column, out string columnName, out string? qualifier))            
                return;

            if (!TryReadTargetTypeName(typeCast, out string targetTypeName))            
                return;            
            
            summary.CastOnColumnOccurrences.Add(
                new CastOnColumnOccurrence
                {
                    ColumnName = columnName,
                    Qualifier = qualifier,
                    TargetTypeName = targetTypeName,
                    Location = pathFormatter.Format(context),
                    SourceOffset = typeCast.Location >= 0 ? typeCast.Location : null
                }
            );
        }       

        private static bool TryReadTargetTypeName(TypeCast typeCast, out string targetTypeName)
        {
            targetTypeName = string.Empty;

            var typeName = typeCast.TypeName;

            if (typeName is null || typeName.Names.Count == 0)
                return false;

            var parts = new List<string>();

            foreach (var name in typeName.Names)
            {
                if (name.String is null)
                    return false;

                parts.Add(name.String.Sval);
            }
            targetTypeName = string.Join(".", parts);
            return true;
        }
    }
}