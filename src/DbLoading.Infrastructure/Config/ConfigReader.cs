using System.Text.Json;

namespace DbLoading.Infrastructure.Config;

public class ConfigReader
{
    private readonly string _configPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ConfigReader(string configPath)
    {
        _configPath = configPath;
    }

    public string ConfigPath => _configPath;

    public async Task<T> ReadAsync<T>(string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_configPath, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Config file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) 
            ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }

    public T Read<T>(string fileName)
    {
        var filePath = Path.Combine(_configPath, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Config file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) 
            ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }

    public List<DatabaseConfig> LoadDatabases()
    {
        return Read<List<DatabaseConfig>>("databases.json");
    }

    public StreamsConfig LoadStreams()
    {
        return Read<StreamsConfig>("streams.json");
    }

    public ScriptsConfig LoadScripts()
    {
        return Read<ScriptsConfig>("scripts.json");
    }

    public ColumnsConfig LoadColumns()
    {
        return Read<ColumnsConfig>("columns.json");
    }
}
