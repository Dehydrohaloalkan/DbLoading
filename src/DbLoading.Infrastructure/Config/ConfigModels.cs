namespace DbLoading.Infrastructure.Config;

// Models for JSON deserialization

public record AppConfig(
    OutputConfig Output,
    ExecutionConfig Execution,
    RealtimeConfig Realtime,
    AuthConfig Auth
);

public record OutputConfig(
    string RootPath,
    string ScriptsRoot,
    string Encoding,
    long MaxFileBytes,
    string CleanupPolicy,
    bool AllowOversizeSingleLine
);

public record ExecutionConfig(
    int LaneCount
);

public record RealtimeConfig(
    bool SignalrEnabled
);

public record AuthConfig(
    int AccessTokenMinutes,
    int RefreshTokenHours,
    string RefreshCookieName
);

public record DatabaseConfig(
    string Id,
    string DisplayName,
    string Server,
    string Database
);

public record StreamsConfig(
    List<ManagerConfig> Managers,
    List<StreamConfig> Streams
);

public record ManagerConfig(
    string Id,
    string DisplayName
);

public record StreamConfig(
    string Id,
    string DisplayName
);

public record ScriptsConfig(
    List<ScriptGroupConfig> Groups
);

public record ScriptGroupConfig(
    string Id,
    string DisplayName,
    List<ScriptConfig> Scripts
);

public record ScriptConfig(
    string Id,
    string DisplayName,
    int ExecutionLane,
    List<VariantConfig> Variants,
    string? ColumnsProfileId
);

public record VariantConfig(
    string Id,
    string SqlFile
);

public record ColumnsConfig(
    List<ColumnProfileConfig> Profiles,
    SerializationConfig Serialization
);

public record ColumnProfileConfig(
    string Id,
    List<ColumnItemConfig> Items
);

public record ColumnItemConfig(
    string Id,
    string Label,
    string Expression
);

public record SerializationConfig(
    string Delimiter,
    EscapeConfig Escape
);

public record EscapeConfig(
    string Backslash,
    string Pipe,
    string Cr,
    string Lf
);
