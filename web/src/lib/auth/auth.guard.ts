import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthLibService } from './auth.service';

export interface AuthGuardConfig {
  loginRoute?: string;
}

let guardConfig: AuthGuardConfig = { loginRoute: '/login' };

export function configureAuthGuard(config: AuthGuardConfig): void {
  guardConfig = config;
}

export const authLibGuard: CanActivateFn = () => {
  const authService = inject(AuthLibService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree([guardConfig.loginRoute ?? '/login']);
};
