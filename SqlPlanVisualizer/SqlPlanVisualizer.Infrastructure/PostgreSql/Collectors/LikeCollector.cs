using PgSqlParser;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Core.Models.Occurences;
using SqlPlanVisualizer.Infrastructure.PostgreSql.Ast;
using System;
using System.Linq;
using System.Text;

namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class LikeCollector : CollectorBase<A_Expr>
    {
        private readonly AstPathFormatter pathFormatter = new();
        private readonly AstContextResolver contextResolver = new();

        protected override void CollectNode(A_Expr expression, AstVisitContext context, SqlStatementSummary summary)
        {            
            if (expression.Kind != A_Expr_Kind.AexprLike && expression.Kind != A_Expr_Kind.AexprIlike)            
                return;
            
            if (expression.Name.Count != 1)
                return;

            string? operatorName = expression.Name[0].String?.Sval;

            if (operatorName != "~~" && operatorName != "~~*")
                return;
            
            AstClause clause = contextResolver.GetClause(context);

            if (clause != AstClause.Where && clause != AstClause.JoinOn)
                return;
            
            if (!TryReadPattern(expression.Rexpr, out string pattern, out string escape))
                return;
            
            if (!TryAnalyzePattern(pattern, escape, out bool hasWildcard, out bool hasLeadingWildcard))
                return;
            
            summary.LikeOccurrences.Add(new LikeOccurrence
            {
                Pattern = pattern,
                IsCaseInsensitive = operatorName == "~~*",
                HasWildcard = hasWildcard,
                HasLeadingWildcard = hasLeadingWildcard,
                Location = pathFormatter.Format(context),
                SourceOffset = expression.Location >= 0 ? expression.Location : null
            });
        }

        private static bool TryReadPattern(Node? rightOperand, out string pattern, out string escape)
        {
            pattern = string.Empty;            
            escape = "\\";
                        
            if (TryReadStringConstant(rightOperand, out pattern))
                return true;
                        
            var function = rightOperand?.FuncCall;

            if (function is null || function.Funcname.Count != 2 || function.Funcname[0].String?.Sval != "pg_catalog" || function.Funcname[1].String?.Sval != "like_escape" || function.Args.Count != 2)            
                return false;            

            if (!TryReadStringConstant(function.Args[0], out pattern))            
                return false;            

            if (!TryReadStringConstant(function.Args[1], out escape))            
                return false;           

            return true;
        }

        private static bool TryReadStringConstant(Node? node, out string value)
        {
            value = string.Empty;

            var constant = node?.AConst;

            if (constant is null || constant.Isnull || constant.Sval is null)            
                return false;            

            value = constant.Sval.Sval;
            return true;
        }

        private static bool TryAnalyzePattern(string pattern, string escape, out bool hasWildcard, out bool hasLeadingWildcard)
        {
            hasWildcard = false;
            hasLeadingWildcard = false;
            
            var patternCharacters = pattern.EnumerateRunes().ToArray();
            var escapeCharacters = escape.EnumerateRunes().ToArray();

            if (escapeCharacters.Length > 1)
                return false;

            bool escapeEnabled = escapeCharacters.Length == 1;

            for (int i = 0; i < patternCharacters.Length; i++)
            {
                var character = patternCharacters[i];
            
                if (escapeEnabled &&
                    character == escapeCharacters[0])
                {                    
                    if (i + 1 >= patternCharacters.Length)
                        return false;
                    i++;
                    continue;
                }

                bool isWildcard =
                    character.Value == '%' ||
                    character.Value == '_';

                if (!isWildcard)
                    continue;

                hasWildcard = true;

                if (i == 0)
                    hasLeadingWildcard = true;
            }

            return true;
        }
    }
}