import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { authLibInterceptor, configureAuthInterceptor, AuthLibService } from '../../lib/auth';

let configured = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authLib = inject(AuthLibService);

  if (!configured) {
    configureAuthInterceptor({
      onUnauthorized: () => {
        const currentUrl = router.url;
        if (currentUrl !== '/login') {
          authLib.logout().subscribe({ next: () => {}, error: () => {} });
          router.navigate(['/login']);
        }
      }
    });
    configured = true;
  }

  return authLibInterceptor(req, next);
};
