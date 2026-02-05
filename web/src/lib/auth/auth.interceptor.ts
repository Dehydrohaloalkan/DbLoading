import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthLibService } from './auth.service';

export interface AuthInterceptorConfig {
  excludePaths?: string[];
  onUnauthorized?: () => void;
}

let interceptorConfig: AuthInterceptorConfig = {};

export function configureAuthInterceptor(config: AuthInterceptorConfig): void {
  interceptorConfig = config;
}

export const authLibInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthLibService);
  
  const isExcluded = interceptorConfig.excludePaths?.some(path => req.url.includes(path)) ?? false;
  const isAuthEndpoint = req.url.includes('/auth/login') || 
                         req.url.includes('/auth/refresh') || 
                         req.url.includes('/auth/logout');

  const accessToken = authService.getAccessToken();
  
  if (accessToken && !isAuthEndpoint && !isExcluded) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${accessToken}`
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isAuthEndpoint && !isExcluded) {
        return authService.refresh().pipe(
          switchMap((response) => {
            const newReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${response.accessToken}`
              }
            });
            return next(newReq);
          }),
          catchError((refreshError) => {
            authService.clearAuth();
            interceptorConfig.onUnauthorized?.();
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
