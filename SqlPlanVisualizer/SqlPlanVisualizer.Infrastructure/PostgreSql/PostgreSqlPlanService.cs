using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    /// <summary>
    /// Az EXPLAIN JSON tartalmát végrehajtási tervekké alakítja.
    /// </summary>
    public class PostgreSqlPlanService
    {
        private readonly PostgreSqlPlanParser _parser = new();
        private readonly PostgreSqlPlanMapper _mapper = new();

        /// <summary>
        /// Ellenőrzi a JSON-t, majd előállítja a terveket az időadataikkal együtt.
        /// </summary>
        public List<ExecutionPlan> ParsePlans(string json)
        {
            var parsedPlans = _parser.Parse(json);
            var plans = new List<ExecutionPlan>();

            foreach (var dto in parsedPlans)
            {
                var root = _mapper.MapNode(dto.Plan!);

                plans.Add(new ExecutionPlan(plans.Count + 1, root)
                {
                    PlanningTime = dto.PlanningTime,
                    ExecutionTime = dto.ExecutionTime
                });
            }

            return plans;
        }
    }
}