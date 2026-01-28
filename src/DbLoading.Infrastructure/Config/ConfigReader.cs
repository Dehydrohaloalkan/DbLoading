using System.Text.Json;

namespace DbLoading.Infrastructure.Config;

public class ConfigReader
{
    private readonly string _configPath;

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
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }
}
