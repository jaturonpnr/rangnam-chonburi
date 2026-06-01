import { Component, OnInit, signal, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminServiceRequest, ServiceRequestStatus } from '../../../core/models';

@Component({
  selector: 'app-service-requests',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './service-requests.component.html'
})
export class ServiceRequestsComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  requests = signal<AdminServiceRequest[]>([]);
  filterStatus = '';

  ngOnInit() { this.load(); }

  load() {
    this.api.getAdminServiceRequests(this.filterStatus || undefined)
      .subscribe(r => this.requests.set(r.items));
  }

  updateStatus(id: number, status: ServiceRequestStatus) {
    this.api.updateServiceRequestStatus(id, status).subscribe(() => this.load());
  }

  typeLabel(t: string) { return ({ WarrantyClaim: 'เคลมประกัน', Maintenance: 'บริการ/ซ่อม', Other: 'อื่นๆ' } as any)[t] ?? t; }
  statusLabel(s: string) { return ({ New: 'ใหม่', Contacted: 'ติดต่อแล้ว', Done: 'เสร็จสิ้น' } as any)[s] ?? s; }
  logout() { this.auth.logout(); this.router.navigate(['/admin/login']); }
}
