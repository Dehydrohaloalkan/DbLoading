namespace DbLoading.Auth.Models;

public record LoginRequest(
    string Username,
    string Password,
    Dictionary<string, string>? CustomClaims = null
);
