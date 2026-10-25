using System.Collections.Generic;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class PossibleCartesianProductCollector : CollectorBase<SelectStmt>
    {
        private readonly AstPathFormatter pathFormatter = new();

        protected override void CollectNode(SelectStmt select, AstVisitContext context, SqlStatementSummary summary)
        {            
            if (select.Op != SetOperation.SetopNone)
                return;

            if (select.FromClause.Count < 2)
                return;

            if (select.WhereClause != null || select.HavingClause != null)
                return;

            var relations = new List<string>();
            int? sourceOffset = null;

            foreach (var item in select.FromClause)
            {
                var relation = item.RangeVar;

                if (relation is null)
                    return;

                string name = relation.Relname;

                if (!string.IsNullOrEmpty(relation.Schemaname))
                    name = $"{relation.Schemaname}.{name}";

                if (!string.IsNullOrEmpty(relation.Catalogname))
                    name = $"{relation.Catalogname}.{name}";

                string? alias = relation.Alias?.Aliasname;

                if (!string.IsNullOrEmpty(alias))
                    name = $"{name} AS {alias}";

                relations.Add(name);

                if (sourceOffset is null && relation.Location >= 0)
                    sourceOffset = relation.Location;
            }

            summary.PossibleCartesianProducts.Add(new PossibleCartesianProductOccurrence
            {
                Relations = relations,
                Location = pathFormatter.Format(context),
                SourceOffset = sourceOffset
            });
        }
    }
}