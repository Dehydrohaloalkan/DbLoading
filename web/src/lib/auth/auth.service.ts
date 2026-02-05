import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { AuthConfig, LoginRequest, LoginResponse, RefreshResponse, UserInfo } from './models';

@Injectable()
export class AuthLibService {
  private readonly accessToken = signal<string | null>(null);
  private readonly currentUser = signal<UserInfo | null>(null);
  private refreshInProgress = false;
  private config: AuthConfig = { apiUrl: '/api/auth' };

  readonly accessToken$ = this.accessToken.asReadonly();
  readonly currentUser$ = this.currentUser.asReadonly();

  constructor(private readonly http: HttpClient) {}

  configure(config: AuthConfig): void {
    this.config = config;
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.config.apiUrl}/login`, request, {
      withCredentials: true
    }).pipe(
      tap(response => {
        this.accessToken.set(response.accessToken);
        this.currentUser.set(response.user);
      }),
      catchError(error => {
        this.clearAuth();
        return throwError(() => error);
      })
    );
  }

  refresh(): Observable<RefreshResponse> {
    if (this.refreshInProgress) {
      return new Observable(observer => {
        const checkInterval = setInterval(() => {
          if (!this.refreshInProgress) {
            clearInterval(checkInterval);
            this.http.post<RefreshResponse>(`${this.config.apiUrl}/refresh`, {}, {
              withCredentials: true
            }).subscribe({
              next: (response) => {
                this.accessToken.set(response.accessToken);
                observer.next(response);
                observer.complete();
              },
              error: (err) => {
                this.clearAuth();
                observer.error(err);
              }
            });
          }
        }, 100);
      });
    }

    this.refreshInProgress = true;
    return this.http.post<RefreshResponse>(`${this.config.apiUrl}/refresh`, {}, {
      withCredentials: true
    }).pipe(
      tap({
        next: (response) => {
          this.accessToken.set(response.accessToken);
          this.refreshInProgress = false;
        },
        error: () => {
          this.clearAuth();
          this.refreshInProgress = false;
        }
      })
    );
  }

  logout(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.config.apiUrl}/logout`, {}, {
      withCredentials: true
    }).pipe(
      tap(() => {
        this.clearAuth();
      }),
      catchError(error => {
        this.clearAuth();
        return throwError(() => error);
      })
    );
  }

  clearAuth(): void {
    this.accessToken.set(null);
    this.currentUser.set(null);
    this.refreshInProgress = false;
  }

  getAccessToken(): string | null {
    return this.accessToken();
  }

  getCurrentUser(): UserInfo | null {
    return this.currentUser();
  }

  isAuthenticated(): boolean {
    return this.accessToken() !== null;
  }

  setTokens(accessToken: string, user: UserInfo): void {
    this.accessToken.set(accessToken);
    this.currentUser.set(user);
  }
}
