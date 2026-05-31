import { Component, signal, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="login-wrap">
      <div class="login-card">
        <div class="login-logo">ADMIN CONSOLE</div>
        <h2 class="login-title">เข้าสู่ระบบ</h2>
        <form [formGroup]="form" (ngSubmit)="login()">
          <div class="form-group">
            <label>ชื่อผู้ใช้</label>
            <input class="form-control" formControlName="username" autocomplete="username" placeholder="username">
          </div>
          <div class="form-group">
            <label>รหัสผ่าน</label>
            <input class="form-control" type="password" formControlName="password" autocomplete="current-password" placeholder="••••••••">
          </div>
          @if (error()) {
            <div class="error-msg" style="margin-bottom: 14px;">⚠ {{ error() }}</div>
          }
          <button type="submit" class="btn btn-primary btn-block" [disabled]="loading()">
            {{ loading() ? 'กำลังตรวจสอบ...' : 'เข้าสู่ระบบ' }}
          </button>
        </form>
      </div>
    </div>
  `
})
export class LoginComponent {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  form = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required]
  });
  loading = signal(false);
  error = signal('');

  login() {
    if (this.form.invalid) return;
    this.loading.set(true);
    const { username, password } = this.form.value;
    this.api.login(username!, password!).subscribe({
      next: r => { this.auth.setToken(r.token); this.router.navigate(['/admin/dashboard']); },
      error: () => { this.error.set('ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง'); this.loading.set(false); }
    });
  }
}
