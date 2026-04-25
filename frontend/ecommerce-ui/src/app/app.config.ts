import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter,withComponentInputBinding  } from '@angular/router';
import { provideHttpClient, withInterceptors,withFetch  } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { tokenInterceptor } from './core/Auth/interceptors/token.interceptor';
import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes,withComponentInputBinding()), provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch(), withInterceptors([tokenInterceptor])),
    provideAnimationsAsync(),
  ]
};
