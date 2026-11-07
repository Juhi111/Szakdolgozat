using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis.CombinedRules;

internal static class PlanShape
{
    public static PlanNode? OuterChild(PlanNode node)
    {
        var children = node.Children.Where(child => PlanFilterMatcher.ReadText(child, "Parent Relationship") is not ("InitPlan" or "SubPlan")).ToArray();
        if (children.Length != 1) return null;
        var role = PlanFilterMatcher.ReadText(children[0], "Parent Relationship");
        return role is null or "Outer" ? children[0] : null;
    }

    public static PlanNode? TopSort(PlanNode root)
    {
        var node = root;
        if (node.NodeType == "Limit") node = OuterChild(node);
        if (node?.NodeType == "Gather Merge") node = OuterChild(node);
        return node?.NodeType == "Sort" ? node : null;
    }

    public static PlanNode? SingleRelationScan(PlanNode root)
    {
        PlanNode? node = root;
        while (node?.NodeType is "Limit" or "Sort" or "Gather" or "Gather Merge" or "Result")
            node = OuterChild(node);
        return node?.NodeType is "Seq Scan" or "Index Scan" or "Index Only Scan" or "Bitmap Heap Scan" ? node : null;
    }
}
