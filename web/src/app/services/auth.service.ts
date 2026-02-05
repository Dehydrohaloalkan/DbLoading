import { HttpClient } from '@angular/common/http';
import { inject, Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthLibService } from '../../lib/auth';

export interface LoginRequest {
  dbUsername: string;
  dbPassword: string;
  databaseId: string;
  managerId: string;
  streamId: string;
}

export interface UserDto {
  login: string;
  databaseId: string;
  managerId: string;
  streamId: string;
}

export interface LoginResponse {
  accessToken: string;
  user: UserDto;
}

export interface RefreshResponse {
  accessToken: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly authLib = inject(AuthLibService);
  private readonly apiUrl = 'http://localhost:5068/api/auth';
  
  private readonly _currentUser = signal<UserDto | null>(null);

  readonly accessToken$ = this.authLib.accessToken$;
  readonly currentUser$ = this._currentUser.asReadonly();

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request, {
      withCredentials: true
    }).pipe(
      tap(response => {
        this.authLib.setTokens(response.accessToken, {
          userId: response.user.login,
          login: response.user.login,
          customClaims: {
            databaseId: response.user.databaseId,
            managerId: response.user.managerId,
            streamId: response.user.streamId
          }
        });
        this._currentUser.set(response.user);
      })
    );
  }

  refresh(): Observable<RefreshResponse> {
    return this.authLib.refresh();
  }

  logout(): Observable<{ message: string }> {
    return this.authLib.logout().pipe(
      tap(() => this._currentUser.set(null))
    );
  }

  clearAuth(): void {
    this.authLib.clearAuth();
    this._currentUser.set(null);
  }

  getAccessToken(): string | null {
    return this.authLib.getAccessToken();
  }

  getCurrentUser(): UserDto | null {
    return this._currentUser();
  }

  isAuthenticated(): boolean {
    return this.authLib.isAuthenticated();
  }
}
