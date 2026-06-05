import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { PortfolioPostAdmin, SavePortfolioPostRequest, CsvImportResult } from '../../../core/models';

@Component({
  selector: 'app-portfolio-posts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './portfolio-posts.component.html',
  styleUrls: ['./portfolio-posts.component.css']
})
export class PortfolioPostsComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  posts = signal<PortfolioPostAdmin[]>([]);
  loading = signal(true);
  csvUploading = signal(false);
  csvResult = signal<CsvImportResult | null>(null);
  csvError = signal<string | null>(null);

  // Bulk
  selectedIds = signal<Set<number>>(new Set());
  bulkUpdating = signal(false);

  // Edit overlay
  editingPost = signal<PortfolioPostAdmin | null>(null);
  editForm: Partial<SavePortfolioPostRequest> = {};
  savingEdit = signal(false);
  editError = signal<string | null>(null);

  // Leaflet
  private editMap: any = null;
  private editMarker: any = null;
  editApproxLat = signal<number | null>(null);
  editApproxLng = signal<number | null>(null);

  ngOnInit() { this.load(); }

  ngOnDestroy() { this.destroyMap(); }

  logout() {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }

  load() {
    this.loading.set(true);
    this.api.getAdminPortfolioPosts().subscribe({
      next: p => { this.posts.set(p); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onCsvSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.csvUploading.set(true); this.csvResult.set(null); this.csvError.set(null);
    this.api.importPortfolioCsv(file).subscribe({
      next: r => { this.csvResult.set(r); this.csvUploading.set(false); this.load(); },
      error: e => { this.csvError.set(e.error?.error ?? 'เกิดข้อผิดพลาด'); this.csvUploading.set(false); }
    });
    (event.target as HTMLInputElement).value = '';
  }

  toggleSelect(id: number) {
    const s = new Set(this.selectedIds());
    s.has(id) ? s.delete(id) : s.add(id);
    this.selectedIds.set(s);
  }

  selectAll() { this.selectedIds.set(new Set(this.posts().map(p => p.id))); }
  clearSelection() { this.selectedIds.set(new Set()); }

  bulkPublish(pub: boolean) {
    const ids = [...this.selectedIds()];
    if (!ids.length) return;
    this.bulkUpdating.set(true);
    this.api.bulkPublishPortfolioPosts(ids, pub).subscribe({
      next: () => { this.bulkUpdating.set(false); this.clearSelection(); this.load(); },
      error: () => this.bulkUpdating.set(false)
    });
  }

  openEdit(post: PortfolioPostAdmin) {
    this.editingPost.set(post);
    this.editForm = {
      fbPostUrl: post.fbPostUrl, title: post.title, areaName: post.areaName,
      approxLat: post.approxLat, approxLng: post.approxLng,
      postedDate: post.postedDate?.substring(0, 10) ?? undefined,
      isPublished: post.isPublished, displayOrder: post.displayOrder
    };
    this.editApproxLat.set(post.approxLat);
    this.editApproxLng.set(post.approxLng);
    this.editError.set(null);
    setTimeout(() => this.initEditMap(), 100);
  }

  openCreate() {
    const blank: PortfolioPostAdmin = { id: 0, fbPostUrl: '', title: null, areaName: null, approxLat: null, approxLng: null, postedDate: null, reach: null, isPublished: false, displayOrder: 0, createdAt: '' };
    this.editingPost.set(blank);
    this.editForm = { fbPostUrl: '', isPublished: false, displayOrder: 0 };
    this.editApproxLat.set(null); this.editApproxLng.set(null);
    this.editError.set(null);
    setTimeout(() => this.initEditMap(), 100);
  }

  cancelEdit() { this.destroyMap(); this.editingPost.set(null); }

  saveEdit() {
    if (!this.editForm.fbPostUrl) { this.editError.set('กรุณากรอก URL โพสต์'); return; }
    const req: SavePortfolioPostRequest = {
      fbPostUrl: this.editForm.fbPostUrl!,
      title: this.editForm.title ?? null,
      areaName: this.editForm.areaName ?? null,
      approxLat: this.editApproxLat(),
      approxLng: this.editApproxLng(),
      postedDate: this.editForm.postedDate ? new Date(this.editForm.postedDate).toISOString() : null,
      isPublished: this.editForm.isPublished ?? false,
      displayOrder: this.editForm.displayOrder ?? 0
    };
    this.savingEdit.set(true);
    const id = this.editingPost()!.id;
    const obs = id === 0 ? this.api.createPortfolioPost(req) : this.api.updatePortfolioPost(id, req);
    obs.subscribe({
      next: () => { this.savingEdit.set(false); this.destroyMap(); this.editingPost.set(null); this.load(); },
      error: e => { this.editError.set(e.error?.error ?? 'เกิดข้อผิดพลาด'); this.savingEdit.set(false); }
    });
  }

  deletePost(id: number) {
    if (!confirm('ลบโพสต์นี้?')) return;
    this.api.deletePortfolioPost(id).subscribe({
      next: () => this.load(),
      error: e => alert(e.error?.error ?? 'ลบไม่สำเร็จ')
    });
  }

  onLatChange(val: number | null) {
    this.editApproxLat.set(val ? +Number(val).toFixed(5) : null);
    this.moveMarker();
  }

  onLngChange(val: number | null) {
    this.editApproxLng.set(val ? +Number(val).toFixed(5) : null);
    this.moveMarker();
  }

  private moveMarker() {
    const lat = this.editApproxLat();
    const lng = this.editApproxLng();
    if (!this.editMap || !lat || !lng) return;
    if (this.editMarker) this.editMarker.remove();
    import('leaflet').then(m => (m as any).default ?? m).then(L => {
      this.editMarker = L.circleMarker([lat, lng], {
        radius: 8, fillColor: '#38bdf8', color: '#0D1B3E', weight: 2, fillOpacity: 0.9
      }).addTo(this.editMap);
      this.editMap.setView([lat, lng]);
    });
  }

  private async initEditMap() {
    const L = await import('leaflet').then(m => (m as any).default ?? m);
    this.destroyMap();
    const el = document.getElementById('pp-edit-map');
    if (!el) return;
    const lat = this.editApproxLat() ?? 13.3435218;
    const lng = this.editApproxLng() ?? 100.9820816;
    this.editMap = L.map(el).setView([lat, lng], 12);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.editMap);
    if (this.editApproxLat() && this.editApproxLng()) {
      this.editMarker = L.circleMarker([lat, lng], {
        radius: 8, fillColor: '#38bdf8', color: '#0D1B3E', weight: 2, fillOpacity: 0.9
      }).addTo(this.editMap);
    }
    this.editMap.on('click', (e: any) => {
      const { lat, lng } = e.latlng;
      this.editApproxLat.set(+lat.toFixed(5));
      this.editApproxLng.set(+lng.toFixed(5));
      if (this.editMarker) this.editMarker.remove();
      this.editMarker = L.circleMarker([lat, lng], {
        radius: 8, fillColor: '#38bdf8', color: '#0D1B3E', weight: 2, fillOpacity: 0.9
      }).addTo(this.editMap);
    });
  }

  private destroyMap() {
    this.editMarker?.remove(); this.editMarker = null;
    this.editMap?.remove(); this.editMap = null;
  }
}
