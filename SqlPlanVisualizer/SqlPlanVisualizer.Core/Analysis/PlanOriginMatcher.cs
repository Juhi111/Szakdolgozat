using System.Security.Cryptography;
using System.Text;
using SqlPlanVisualizer.Core.Models;

namespace SqlPlanVisualizer.Core.Analysis;

public static class PlanOriginMatcher
{
    public static string HashDocument(string sql) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));

    /// <summary>Csak az azonos dokumentum azonos sorszámú, változatlan utasítását fogadja el.</summary>
    public static bool Matches(PlanOrigin? origin, string document, SqlStatementSummary statement)
    {
        if (origin?.Source != PlanSourceKind.Database || origin.SqlText is null ||
            origin.StatementIndex != statement.StatementIndex || origin.DocumentHash != HashDocument(document))
            return false;

        if (statement.TextStart is not int start || start < 0 || start > document.Length ||
            statement.TextLength <= 0 || statement.TextLength > document.Length - start)
            return false;

        return string.Equals(origin.SqlText, document.Substring(start, statement.TextLength), StringComparison.Ordinal);
    }
}
