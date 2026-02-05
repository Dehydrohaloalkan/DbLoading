export interface LoginRequest {
  username: string;
  password: string;
  customClaims?: Record<string, string>;
}

export interface UserInfo {
  userId: string;
  login: string;
  customClaims: Record<string, string>;
}

export interface LoginResponse {
  accessToken: string;
  user: UserInfo;
}

export interface RefreshResponse {
  accessToken: string;
}

export interface AuthConfig {
  apiUrl: string;
  refreshCookieName?: string;
}
