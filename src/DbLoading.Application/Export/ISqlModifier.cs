namespace DbLoading.Application.Export;

public interface ISqlModifier
{
    string Modify(string sql, SqlModifierContext? context);
}

public sealed class SqlModifierContext
{
    public IReadOnlyList<string> SelectedColumnItemIds { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> IdToExpression { get; init; } = new Dictionary<string, string>();
    public string Delimiter { get; init; } = "|";
    public EscapeRules Escape { get; init; } = new();
}

public sealed class EscapeRules
{
    public string Backslash { get; init; } = "\\\\";
    public string Pipe { get; init; } = "\\|";
    public string Cr { get; init; } = "\\\\r";
    public string Lf { get; init; } = "\\\\n";
}
