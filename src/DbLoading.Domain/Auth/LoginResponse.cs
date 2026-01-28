namespace DbLoading.Domain.Auth;

public record LoginResponse(
    string AccessToken,
    UserDto User
);

public record UserDto(
    string Login,
    string DatabaseId,
    string ManagerId,
    string StreamId
);
