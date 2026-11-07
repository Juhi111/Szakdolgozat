using Google.Protobuf;
using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using System.Text.Json;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;

/// <summary>
/// Szűk, konzervatív AST-egyeztetés. Nem értékel kódot, nem lép allekérdezésbe,
/// és nem próbál általános algebrai ekvivalenciát bizonyítani.
/// </summary>
internal static class ExpressionFingerprint
{
    public static string? Read(IMessage? message, SqlRelationSummary relation, int depth = 0)
    {
        if (message is null || depth > 64) return null;
        string? Child(IMessage? child) => Read(child, relation, depth + 1);
        string? Many(string kind, IEnumerable<Node> nodes) => Pack(kind, nodes.Select(Child).ToArray());

        switch (message)
        {
            case Node node:
                foreach (var field in Node.Descriptor.Fields.InFieldNumberOrder())
                    if (field.Accessor.GetValue(node) is IMessage value) return Child(value);
                return null;
            case ColumnRef column:
                var parts = column.Fields.Select(f => f.String?.Sval).ToArray();
                if (parts.Length == 0 || parts.Any(p => p is null)) return null;
                bool matches = parts.Length == 1 ||
                    (parts.Length == 2 && parts[0] == (relation.Alias ?? relation.RelationName)) ||
                    (parts.Length == 3 && relation.Alias is null && relation.SchemaName is not null &&
                     parts[0] == relation.SchemaName && parts[1] == relation.RelationName);
                return matches ? Pack("column", parts[^1]) : null;
            case A_Const constant:
                if (constant.Isnull) return Pack("null");
                if (constant.Sval is not null) return Pack("string", constant.Sval.Sval);
                if (constant.Ival is not null) return Pack("integer", constant.Ival.Ival.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (constant.Boolval is not null) return Pack("boolean", constant.Boolval.Boolval.ToString());
                return null;
            case ParamRef parameter:
                return Pack("parameter", parameter.Number.ToString(System.Globalization.CultureInfo.InvariantCulture));
            case FuncCall function:
                var names = function.Funcname.Select(n => n.String?.Sval).ToArray();
                if (names.Length == 2 && names[0] == "pg_catalog") names = names.Skip(1).ToArray();
                if (names.Length != 1 || names[0] is not ("lower" or "upper" or "extract" or "date_part") ||
                    function.AggStar || function.AggDistinct || function.AggWithinGroup ||
                    function.Over is not null || function.AggFilter is not null || function.AggOrder.Count != 0) return null;
                return Many("function:" + names[0], function.Args);
            case TypeCast cast:
                var type = cast.TypeName;
                if (type is null || type.ArrayBounds.Count != 0 || type.Typmods.Count != 0 || type.PctType || type.Setof) return null;
                var typeParts = type.Names.Select(n => n.String?.Sval).ToArray();
                if (typeParts.Length == 2 && typeParts[0] == "pg_catalog") typeParts = typeParts.Skip(1).ToArray();
                if (typeParts.Length != 1 || typeParts[0] is null) return null;
                // EXPLAIN kiírja a szövegliterálok ::text típusát. Más castot nem tüntetünk el.
                if (typeParts[0] == "text" && cast.Arg?.AConst?.Sval is not null) return Child(cast.Arg);
                return Pack("cast:" + typeParts[0], Child(cast.Arg));
            case A_Expr expression:
                if (expression.Name.Count != 1 || expression.Name[0].String is null ||
                    expression.Kind is not (A_Expr_Kind.AexprOp or A_Expr_Kind.AexprLike or A_Expr_Kind.AexprIlike)) return null;
                return Pack("operator:" + expression.Name[0].String.Sval, Child(expression.Lexpr), Child(expression.Rexpr));
            case BoolExpr boolean:
                return Many("boolean:" + boolean.Boolop, boolean.Args);
            case NullTest test:
                if (test.Argisrow) return null;
                return Pack("nulltest:" + test.Nulltesttype, Child(test.Arg));
            case CaseExpr expression:
                if (expression.Arg is not null) return null;
                return Pack("case", Many("branches", expression.Args), Child(expression.Defresult));
            case CaseWhen branch:
                return Pack("when", Child(branch.Expr), Child(branch.Result));
            default:
                return null;
        }
    }

    private static string? Pack(string kind, params string?[] parts)
    {
        return parts.Any(p => p is null) ? null : JsonSerializer.Serialize(new[] { kind }.Concat(parts));
    }
}
