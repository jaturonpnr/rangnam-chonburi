import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'admin_token';
  isLoggedIn = signal(!!sessionStorage.getItem(this.TOKEN_KEY));

  setToken(token: string) {
    sessionStorage.setItem(this.TOKEN_KEY, token);
    this.isLoggedIn.set(true);
  }

  getToken() {
    return sessionStorage.getItem(this.TOKEN_KEY);
  }

  logout() {
    sessionStorage.removeItem(this.TOKEN_KEY);
    this.isLoggedIn.set(false);
  }
}
