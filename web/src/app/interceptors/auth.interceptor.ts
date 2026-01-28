import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const accessToken = authService.getAccessToken();
  const isAuthEndpoint = req.url.includes('/auth/login') || 
                         req.url.includes('/auth/refresh') || 
                         req.url.includes('/auth/logout');
  
  if (accessToken && !isAuthEndpoint) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${accessToken}`
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isAuthEndpoint) {
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
            const currentUrl = router.url;
            if (currentUrl !== '/login') {
              authService.logout().subscribe({
                next: () => {},
                error: () => {}
              });
              router.navigate(['/login']);
            }
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
