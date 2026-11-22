using Google.Protobuf;
using PgSqlParser;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Parsers;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql.Rewriting;

/// <summary>Közös AST-segédek. Nem szövegcserével módosítják az SQL-t.</summary>
internal static class RewriteAst
{
    public static ParseResult Parse(string sql) => new PostgreSqlQueryParser().Parse(sql);
    public static Node Expression(string sql) => Parse("SELECT 1 WHERE " + sql).Stmts[0].Stmt.SelectStmt.WhereClause;
    public static string Op(A_Expr? expr) => expr?.Kind == A_Expr_Kind.AexprOp && expr.Name.Count == 1 ? expr.Name[0].String?.Sval ?? "" : "";
    public static string Name(FuncCall function) => function.Funcname.Count == 1 ? function.Funcname[0].String?.Sval ?? "" :
        function.Funcname.Count == 2 && function.Funcname[0].String?.Sval == "pg_catalog" ? function.Funcname[1].String?.Sval ?? "" : "";
    public static bool Column(Node? node) => node?.ColumnRef is { } column && column.Fields.Count > 0 && column.Fields.All(f => f.String is not null);
    public static bool SameColumn(Node? a, Node? b) => Column(a) && Column(b) &&
        a!.ColumnRef.Fields.Select(f => f.String.Sval).SequenceEqual(b!.ColumnRef.Fields.Select(f => f.String.Sval));
    public static bool Integer(Node? node, out int value)
    {
        value = node?.AConst?.Ival?.Ival ?? 0;
        return node?.AConst is { Isnull: false, Ival: not null };
    }
    public static bool Text(Node? node, out string value)
    {
        value = node?.AConst?.Sval?.Sval ?? "";
        return node?.AConst is { Isnull: false, Sval: not null };
    }
    public static bool Literal(Node node) => node.AConst is { Isnull: false } c && (c.Ival is not null || c.Sval is not null);
    public static string LiteralKey(Node node) => node.AConst.Ival is not null ? "i:" + node.AConst.Ival.Ival : "s:" + node.AConst.Sval.Sval;
    public static bool Homogeneous(IEnumerable<Node> values)
    {
        var list = values.ToList();
        return list.Count > 0 && list.All(Literal) && list.All(n => (n.AConst.Ival is not null) == (list[0].AConst.Ival is not null));
    }
    public static Node Binary(Node left, string op, Node right)
    {
        var node = Expression("0 = 0");
        node.AExpr.Name[0].String.Sval = op;
        node.AExpr.Lexpr = left.Clone();
        node.AExpr.Rexpr = right.Clone();
        return node;
    }
    public static Node And(IEnumerable<Node> nodes)
    {
        var list = nodes.ToList();
        if (list.Count == 1) return list[0];
        var expression = new BoolExpr { Boolop = BoolExprType.AndExpr };
        expression.Args.Add(list);
        return new Node { BoolExpr = expression };
    }
    public static string ColumnSql(Node node) => string.Join(".", node.ColumnRef.Fields.Select(f => "\"" + f.String.Sval.Replace("\"", "\"\"") + "\""));
    public static bool Type(TypeName type, string name) => type.ArrayBounds.Count == 0 && type.Typmods.Count == 0 &&
        (type.Names.Count == 1 && type.Names[0].String?.Sval == name || type.Names.Count == 2 &&
         type.Names[0].String?.Sval == "pg_catalog" && type.Names[1].String?.Sval == name);
    public static bool Supported(SelectStmt? select) => select is not null && select.Op == SetOperation.SetopNone &&
        select.WithClause is null && select.IntoClause is null && select.LockingClause.Count == 0 &&
        select.FromClause.Count == 1 && select.FromClause[0].RangeVar is not null;
    public static bool Contains(IMessage node, Func<IMessage, bool> predicate)
    {
        bool found = false;
        new PostgreSqlAstWalker().Walk(node, 1, entry => { if (predicate(entry.Node)) found = true; });
        return found;
    }
    public static List<(Node Node, Action<Node> Replace)> Slots(SelectStmt select, bool having = false)
    {
        var result = new List<(Node, Action<Node>)>();
        void Visit(Node? node, Action<Node> replace)
        {
            if (node is null) return;
            if (node.BoolExpr is { Boolop: BoolExprType.AndExpr } boolean)
            {
                for (int i = 0; i < boolean.Args.Count; i++) { int index = i; Visit(boolean.Args[i], n => boolean.Args[index] = n); }
            }
            else result.Add((node, replace));
        }
        if (having) Visit(select.HavingClause, n => select.HavingClause = n);
        else Visit(select.WhereClause, n => select.WhereClause = n);
        return result;
    }
}
