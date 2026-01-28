namespace DbLoading.Domain.Auth;

public class UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string ManagerId { get; set; } = string.Empty;
    public string StreamId { get; set; } = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    public int SessionVersion { get; set; }
    public DateTime CreatedAt { get; set; }
}
