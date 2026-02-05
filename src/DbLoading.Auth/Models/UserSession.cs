namespace DbLoading.Auth.Models;

public class UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    public int SessionVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> CustomClaims { get; set; } = new();
}
