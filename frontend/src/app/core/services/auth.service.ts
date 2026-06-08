import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'admin_token';
  private platformId = inject(PLATFORM_ID);
  isLoggedIn = signal(isPlatformBrowser(this.platformId) ? !!sessionStorage.getItem(this.TOKEN_KEY) : false);

  setToken(token: string) {
    sessionStorage.setItem(this.TOKEN_KEY, token);
    this.isLoggedIn.set(true);
  }

  getToken() {
    if (!isPlatformBrowser(this.platformId)) return null;
    return sessionStorage.getItem(this.TOKEN_KEY);
  }

  logout() {
    if (!isPlatformBrowser(this.platformId)) return;
    sessionStorage.removeItem(this.TOKEN_KEY);
    this.isLoggedIn.set(false);
  }
}
