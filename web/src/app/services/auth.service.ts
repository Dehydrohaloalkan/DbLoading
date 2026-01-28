import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

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
  private readonly apiUrl = 'http://localhost:5068/api/auth';

  private accessToken = signal<string | null>(null);
  private currentUser = signal<UserDto | null>(null);
  private refreshInProgress = false;

  readonly accessToken$ = this.accessToken.asReadonly();
  readonly currentUser$ = this.currentUser.asReadonly();

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request, {
      withCredentials: true
    }).pipe(
      tap(response => {
        this.accessToken.set(response.accessToken);
        this.currentUser.set(response.user);
      })
    );
  }

  refresh(): Observable<RefreshResponse> {
    if (this.refreshInProgress) {
      return new Observable(observer => {
        const checkInterval = setInterval(() => {
          if (!this.refreshInProgress) {
            clearInterval(checkInterval);
            this.http.post<RefreshResponse>(`${this.apiUrl}/refresh`, {}, {
              withCredentials: true
            }).subscribe({
              next: (response) => {
                this.accessToken.set(response.accessToken);
                observer.next(response);
                observer.complete();
              },
              error: (err) => {
                this.logout();
                observer.error(err);
              }
            });
          }
        }, 100);
      });
    }

    this.refreshInProgress = true;
    return this.http.post<RefreshResponse>(`${this.apiUrl}/refresh`, {}, {
      withCredentials: true
    }).pipe(
      tap({
        next: (response) => {
          this.accessToken.set(response.accessToken);
          this.refreshInProgress = false;
        },
        error: () => {
          this.logout();
          this.refreshInProgress = false;
        }
      })
    );
  }

  logout(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/logout`, {}, {
      withCredentials: true
    }).pipe(
      tap(() => {
        this.clearAuth();
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

  getCurrentUser(): UserDto | null {
    return this.currentUser();
  }

  isAuthenticated(): boolean {
    return this.accessToken() !== null;
  }
}
