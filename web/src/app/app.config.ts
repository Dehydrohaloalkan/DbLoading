import { ApplicationConfig, provideBrowserGlobalErrorListeners, APP_INITIALIZER } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { AuthLibService } from '../lib/auth';

function initializeAuthLib(authLib: AuthLibService) {
  return () => {
    authLib.configure({
      apiUrl: 'http://localhost:5068/api/auth'
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    AuthLibService,
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuthLib,
      deps: [AuthLibService],
      multi: true
    }
  ]
};
