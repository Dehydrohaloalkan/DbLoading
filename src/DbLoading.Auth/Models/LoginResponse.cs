namespace DbLoading.Auth.Models;

public record LoginResponse(
    string AccessToken,
    UserInfo User
);

public record UserInfo(
    string UserId,
    string Login,
    Dictionary<string, string> CustomClaims
);
