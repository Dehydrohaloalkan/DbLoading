using System.Text.RegularExpressions;
using DbLoading.Application.Export;

namespace DbLoading.Infrastructure.Export;

public sealed class SqlModifierService : ISqlModifier
{
    private static readonly Regex LineFilePattern = new(
        @"\bSELECT\s+""LineFile""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Modify(string sql, SqlModifierContext? context)
    {
        if (context == null || context.SelectedColumnItemIds.Count == 0)
            return sql;

        var match = LineFilePattern.Match(sql);
        if (!match.Success)
            throw new InvalidOperationException("Script is not modifiable: SQL must start with SELECT \"LineFile\".");

        var quoteStart = sql.IndexOf("\"LineFile\"", StringComparison.Ordinal);
        if (quoteStart < 0)
            throw new InvalidOperationException("Script is not modifiable: \"LineFile\" not found.");

        var replacement = "\"LineFile\"";
        var replacementEnd = quoteStart + replacement.Length;

        var parts = new List<string>();
        foreach (var id in context.SelectedColumnItemIds)
        {
            if (!context.IdToExpression.TryGetValue(id, out var expr))
                continue;
            var escaped = WrapCoalesceAndEscape(expr, context.Escape);
            parts.Add(escaped);
        }

        if (parts.Count == 0)
            throw new InvalidOperationException("Script is not modifiable: no valid column expressions.");

        var delimiter = context.Delimiter.Replace("'", "''");
        var concat = string.Join($" || '{delimiter}' || ", parts);

        var newExpr = $"({concat})";
        return sql.Remove(quoteStart, replacementEnd - quoteStart).Insert(quoteStart, newExpr);
    }

    private static string WrapCoalesceAndEscape(string expr, EscapeRules e)
    {
        var inner = $"COALESCE(CAST({expr} AS VARCHAR(4000)), '')";
        var b = e.Backslash.Replace("'", "''");
        var p = e.Pipe.Replace("'", "''");
        var r = e.Cr.Replace("'", "''");
        var n = e.Lf.Replace("'", "''");
        inner = $"REPLACE(REPLACE(REPLACE(REPLACE({inner}, '\\', '{b}'), '|', '{p}'), CHR(13), '{r}'), CHR(10), '{n}')";
        return inner;
    }
}
