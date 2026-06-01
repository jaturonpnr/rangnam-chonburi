import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { QuoteRequestDetail, BreakdownItem, JobDetail } from '../../../core/models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-lead-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './lead-detail.component.html'
})
export class LeadDetailComponent implements OnInit, OnDestroy {
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

  private miniMap: any = null;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.api.getQuoteRequestDetail(id).subscribe(d => {
      this.detail.set(d);
      this.selectedStatus = d.status;
      try { this.breakdown.set(JSON.parse(d.breakdownJson)); } catch { }
      if (d.measuredGeoJson) {
        setTimeout(() => this.initMiniMap(d), 150);
      }
    });
  }

  ngOnDestroy() {
    this.miniMap?.remove();
  }

  private async initMiniMap(d: QuoteRequestDetail) {
    const L = await import('leaflet');
    const el = document.getElementById('admin-mini-map');
    if (!el || this.miniMap) return;

    const map = L.map(el, { zoomControl: true });
    this.miniMap = map;

    L.tileLayer(environment.satelliteTileUrl, {
      attribution: environment.satelliteAttribution,
      maxZoom: 20,
      maxNativeZoom: 18
    }).addTo(map);

    try {
      const geojson = JSON.parse(d.measuredGeoJson!);
      const layer = L.geoJSON(geojson, { style: { color: '#0284C7', weight: 3, opacity: 0.9 } }).addTo(map);
      map.fitBounds(layer.getBounds(), { padding: [24, 24] });
    } catch {
      if (d.mapCenterLat && d.mapCenterLng) {
        map.setView([d.mapCenterLat, d.mapCenterLng], d.mapZoom ?? 16);
      }
    }
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
  copyPhone() { navigator.clipboard.writeText(this.detail()!.phone); }
  logout() { this.auth.logout(); this.router.navigate(['/admin/login']); }

  showCompleteForm = signal(false);
  completing = signal(false);
  hasJob = signal(false);
  completeForm = { installedDate: new Date().toISOString().split('T')[0], warrantyMonths: 12, lat: '', lng: '', areaName: '' };

  completeJob() {
    if (!this.detail()) return;
    this.completing.set(true);
    this.api.completeJob(this.detail()!.id, {
      installedDate: this.completeForm.installedDate,
      warrantyMonths: this.completeForm.warrantyMonths,
      lat: this.completeForm.lat ? +this.completeForm.lat : null,
      lng: this.completeForm.lng ? +this.completeForm.lng : null,
      areaName: this.completeForm.areaName || null
    }).subscribe({
      next: (job: JobDetail) => {
        this.completing.set(false);
        this.showCompleteForm.set(false);
        this.hasJob.set(true);
        this.router.navigate(['/admin/jobs', job.id]);
      },
      error: () => this.completing.set(false)
    });
  }
}
