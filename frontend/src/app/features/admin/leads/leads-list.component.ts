import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { QuoteRequestSummary } from '../../../core/models';

@Component({
  selector: 'app-leads-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './leads-list.component.html'
})
export class LeadsListComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  items = signal<QuoteRequestSummary[]>([]);
  total = signal(0);
  page = signal(1);
  pageSize = 20;
  loading = signal(false);

  filterStatus = '';
  filterFrom = '';
  filterTo = '';

  statuses = ['', 'New', 'Contacted', 'Quoted', 'Won', 'Lost'];
  statusLabels: Record<string, string> = { '': 'ทั้งหมด', New: 'ใหม่', Contacted: 'ติดต่อแล้ว', Quoted: 'ส่งใบเสนอราคา', Won: 'ปิดงานได้', Lost: 'ไม่สำเร็จ' };

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.getQuoteRequests({
      status: this.filterStatus || undefined,
      from: this.filterFrom || undefined,
      to: this.filterTo || undefined,
      page: this.page(),
      pageSize: this.pageSize
    }).subscribe(r => {
      this.items.set(r.items);
      this.total.set(r.total);
      this.loading.set(false);
    });
  }

  applyFilter() { this.page.set(1); this.load(); }
  nextPage() { if (this.page() * this.pageSize < this.total()) { this.page.update(p => p + 1); this.load(); } }
  prevPage() { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  totalPages() { return Math.ceil(this.total() / this.pageSize); }

  logout() { this.auth.logout(); this.router.navigate(['/admin/login']); }
  formatNumber(n: number) { return n.toLocaleString('th-TH'); }
  formatDate(d: string) { return new Date(d).toLocaleDateString('th-TH'); }
}
