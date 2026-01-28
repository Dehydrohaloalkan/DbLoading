using DbLoading.Domain.Auth;

namespace DbLoading.Domain.Runs;

public class Run
{
    public string RunId { get; set; } = string.Empty;
    public UserContext UserContext { get; set; } = null!;
    public RunSelection Selection { get; set; } = null!;
    public RunStatus Status { get; set; }
    public Dictionary<string, GroupStatus> GroupStatuses { get; set; } = new();
    public Dictionary<string, Dictionary<string, ScriptStatus>> ScriptStatuses { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserContext
{
    public string Login { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string ManagerId { get; set; } = string.Empty;
    public string StreamId { get; set; } = string.Empty;
}

public class RunSelection
{
    public RunMode Mode { get; set; }
    public List<GroupSelection> Groups { get; set; } = new();
}

public enum RunMode
{
    AllGroups
}

public class GroupSelection
{
    public string GroupId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public List<ScriptSelection> Scripts { get; set; } = new();
}

public class ScriptSelection
{
    public string ScriptId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public ExportMode ExportMode { get; set; }
    public List<string> SelectedColumnItemIds { get; set; } = new();
}

public enum ExportMode
{
    DefaultColumns,
    CustomColumns
}