using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Collectors
{
    /// <summary>
    /// Összegyűjti a felső szintű lekérdezés szerkezeti jellemzőit.
    /// </summary>
    public class QueryBlockCollector : CollectorBase<SelectStmt>
    {
        protected override void CollectNode(SelectStmt select, AstVisitContext context, SqlStatementSummary summary)
        {
            var ancestor = context.Parent;

            while (ancestor is not null)
            {
                if (ancestor.NodeType == typeof(SelectStmt))
                    return;

                ancestor = ancestor.Parent;
            }

            var query = new SqlQueryBlockSummary
            {
                IsSetOperation = select.Op != SetOperation.SetopNone,
                IsUnionDistinct = select.Op == SetOperation.SetopUnion && !select.All,
                HasGroupBy = select.GroupClause.Count > 0,
                OffsetSourceOffset = select.LimitOffset?.AConst is { Location: >= 0 } offset ? offset.Location : null,
                HasDistinct = select.DistinctClause.Count > 0,
                OrdersByRandom = select.SortClause.Any(item => IsRandom(item.SortBy?.Node?.FuncCall)),
                HasOrderBy = select.SortClause.Count > 0,
                HasRowLimit = select.LimitCount is not null && select.LimitCount.AConst?.Isnull != true,
                HasOnlyDirectRelations = HasOnlyDirectRelations(select)
            };

            foreach (var item in select.FromClause)
            {
                var relation = item.RangeVar;

                if (relation is null)
                    continue;

                query.Relations.Add(new SqlRelationSummary
                {
                    RelationName = relation.Relname,
                    SchemaName = string.IsNullOrEmpty(relation.Schemaname) ? null : relation.Schemaname,
                    Alias = relation.Alias?.Aliasname
                });
            }

            summary.TopLevelQuery = query;
        }

        /// <summary>
        /// Felismeri a közvetlen, ablak és aggregálás nélküli random() rendezési kulcsot.
        /// </summary>
        private static bool IsRandom(FuncCall? function)
        {
            if (function is null || function.Args.Count != 0 || function.Over is not null ||
                function.AggStar || function.AggDistinct || function.AggWithinGroup ||
                function.AggOrder.Count != 0 || function.AggFilter is not null) return false;
            var names = function.Funcname.Select(n => n.String?.Sval).ToArray();
            return names.SequenceEqual(new[] { "random" }) || names.SequenceEqual(new[] { "pg_catalog", "random" });
        }

        /// <summary>Ellenőrzi, hogy a FROM csak közvetlen táblahivatkozásokat tartalmaz-e.</summary>
        private static bool HasOnlyDirectRelations(SelectStmt select)
        {
            if (select.FromClause.Count == 0 || select.WithClause is not null)
                return false;

            foreach (var item in select.FromClause)
            {
                if (item.RangeVar is null)
                    return false;
            }

            return true;
        }
    }
}