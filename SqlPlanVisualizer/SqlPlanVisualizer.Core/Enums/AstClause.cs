using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlPlanVisualizer.Core.Enums
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
}
