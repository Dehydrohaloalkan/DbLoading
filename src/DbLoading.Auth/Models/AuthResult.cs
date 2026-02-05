namespace DbLoading.Auth.Models;

public record AuthResult<T>(
    bool Success,
    T? Data = default,
    string? Error = null
) where T : class;
