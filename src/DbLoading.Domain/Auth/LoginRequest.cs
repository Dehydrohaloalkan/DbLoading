namespace DbLoading.Domain.Auth;

public record LoginRequest(
    string DbUsername,
    string DbPassword,
    string DatabaseId,
    string ManagerId,
    string StreamId
);
