using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    /// <summary>
    /// Az EXPLAIN JSON tartalmát feldolgozza és tervmodellekké alakítja.
    /// </summary>
    public class PostgreSqlPlanService
    {
        private readonly PostgreSqlPlanParser _parser = new();
        private readonly PostgreSqlPlanMapper _mapper = new();

        /// <summary>
        /// Ellenőrzi a JSON-t, majd előállítja a végrehajtási tervek gyökércsomópontjait.
        /// </summary>
        public List<PlanNode> ParsePlans(string json)
        {
            var plans = _parser.Parse(json);
            var roots = new List<PlanNode>();

            foreach (var plan in plans)
            {
                roots.Add(_mapper.MapNode(plan.Plan!));
            }

            return roots;
        }
    }
}