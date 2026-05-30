import { Component, OnInit, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { QuoteRequestDetail, BreakdownItem } from '../../../core/models';

@Component({
  selector: 'app-lead-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './lead-detail.component.html'
})
export class LeadDetailComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  detail = signal<QuoteRequestDetail | null>(null);
  breakdown = signal<BreakdownItem[]>([]);
  statusUpdating = signal(false);
  selectedStatus = '';

  statuses = ['New', 'Contacted', 'Quoted', 'Won', 'Lost'];
  statusLabels: Record<string, string> = { New: 'ใหม่', Contacted: 'ติดต่อแล้ว', Quoted: 'ส่งใบเสนอราคา', Won: 'ปิดงานได้', Lost: 'ไม่สำเร็จ' };

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.api.getQuoteRequestDetail(id).subscribe(d => {
      this.detail.set(d);
      this.selectedStatus = d.status;
      try { this.breakdown.set(JSON.parse(d.breakdownJson)); } catch { }
    });
  }

  updateStatus() {
    if (!this.detail()) return;
    this.statusUpdating.set(true);
    this.api.updateQuoteStatus(this.detail()!.id, this.selectedStatus).subscribe({
      next: () => { this.detail.update(d => d ? { ...d, status: this.selectedStatus as any } : d); this.statusUpdating.set(false); },
      error: () => this.statusUpdating.set(false)
    });
  }

  pdfUrl() { return this.api.getAdminQuotePdfUrl(this.detail()!.id); }
  formatNumber(n: number | undefined) { return n != null ? n.toLocaleString('th-TH') : '-'; }
  formatDate(d: string) { return new Date(d).toLocaleString('th-TH'); }
  materialLabel(m: string) { return m === 'Galvanized' ? 'สังกะสี' : 'สแตนเลส'; }
  finishLabel(f: string | null) { if (!f) return '-'; return f === 'Glossy' ? 'เงา' : 'ด้าน'; }
  copyPhone() { navigator.clipboard.writeText(this.detail()!.phone); }
  logout() { this.auth.logout(); this.router.navigate(['/admin/login']); }
}
