import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ImportBatchSummary, ImportDraftItem, Material } from '../../../core/models';

@Component({
  selector: 'app-portfolio-import',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './portfolio-import.component.html',
  styleUrl: './portfolio-import.component.css'
})
export class PortfolioImportComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);

  batches = signal<ImportBatchSummary[]>([]);
  selectedBatchId = signal<number | null>(null);
  drafts = signal<ImportDraftItem[]>([]);
  editingDraft = signal<ImportDraftItem | null>(null);

  uploading = signal(false);
  uploadError = signal('');
  fbUploading = signal(false);
  fbUploadResult = signal<{ paired: number; unpaired: number; skipped: number } | null>(null);
  fbUploadError = signal('');
  publishing = signal(false);
  loading = signal(false);

  selectedIds = signal<Set<number>>(new Set<number>());
  bulkAreaName = '';
  bulkUpdating = signal(false);

  editArea = '';
  editMaterial: Material = 'Stainless';
  editSize = 6;
  editLength = 0;
  editShowPortfolio = false;
  editPhotoConsent = true;

  private editMap: any = null;
  private editMapMarker: any = null;
  editApproxLat: number | null = null;
  editApproxLng: number | null = null;

  ngOnInit() {
    this.loadBatches();
  }

  loadBatches() {
    this.api.getImportBatches().subscribe(b => this.batches.set(b));
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const files = Array.from(input.files);
    this.uploading.set(true);
    this.uploadError.set('');
    this.api.importImages(files).subscribe({
      next: r => {
        this.uploading.set(false);
        this.loadBatches();
        this.selectBatch(r.batchId);
        input.value = '';
      },
      error: e => {
        this.uploading.set(false);
        this.uploadError.set(e.error?.error ?? 'อัปโหลดล้มเหลว');
      }
    });
  }

  onFbZipSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    this.fbUploading.set(true);
    this.fbUploadError.set('');
    this.fbUploadResult.set(null);
    this.api.importFbExport(file).subscribe({
      next: r => {
        this.fbUploading.set(false);
        this.fbUploadResult.set({ paired: r.paired, unpaired: r.unpaired, skipped: r.skipped });
        this.loadBatches();
        this.selectBatch(r.batchId);
        input.value = '';
      },
      error: e => {
        this.fbUploading.set(false);
        this.fbUploadError.set(e.error?.error ?? 'นำเข้าล้มเหลว');
      }
    });
  }

  selectBatch(id: number) {
    this.clearSelection();
    this.selectedBatchId.set(id);
    this.loading.set(true);
    this.api.getImportDrafts(id).subscribe(d => {
      this.drafts.set(d);
      this.loading.set(false);
    });
  }

  openEdit(draft: ImportDraftItem) {
    this.editingDraft.set(draft);
    this.editArea = draft.areaName ?? '';
    this.editMaterial = draft.material;
    this.editSize = draft.sizeInches;
    this.editLength = Number(draft.lengthMeters);
    this.editShowPortfolio = draft.showInPortfolio;
    this.editPhotoConsent = draft.photoConsent;
    this.editApproxLat = draft.approxLat ?? null;
    this.editApproxLng = draft.approxLng ?? null;
    this.editMap?.remove();
    this.editMap = null;
    this.editMapMarker = null;
    setTimeout(() => this.initEditMap(), 50);
  }

  private async initEditMap() {
    const el = document.getElementById('pi-edit-map');
    if (!el || this.editMap) return;
    const L = await import('leaflet');
    const center: [number, number] = (this.editApproxLat && this.editApproxLng)
      ? [this.editApproxLat, this.editApproxLng]
      : [13.16, 100.93];
    this.editMap = L.map(el, { center, zoom: 11, scrollWheelZoom: false });
    const { environment } = await import('../../../../environments/environment');
    L.tileLayer(environment.satelliteTileUrl, {
      attribution: environment.satelliteAttribution,
      maxZoom: 20, maxNativeZoom: 18
    }).addTo(this.editMap);
    if (this.editApproxLat && this.editApproxLng) {
      this.editMapMarker = L.marker([this.editApproxLat, this.editApproxLng])
        .addTo(this.editMap);
    }
    this.editMap.on('click', (e: any) => {
      this.editApproxLat = e.latlng.lat;
      this.editApproxLng = e.latlng.lng;
      this.editMapMarker?.remove();
      this.editMapMarker = L.marker([e.latlng.lat, e.latlng.lng]).addTo(this.editMap);
    });
  }

  saveEdit() {
    const draft = this.editingDraft();
    if (!draft) return;
    this.api.updateImportDraft(draft.jobId, {
      areaName: this.editArea || null,
      material: this.editMaterial,
      sizeInches: this.editSize,
      lengthMeters: this.editLength,
      lat: this.editApproxLat,
      lng: this.editApproxLng,
      showInPortfolio: this.editShowPortfolio,
      photoConsent: this.editPhotoConsent
    }).subscribe(() => {
      this.editingDraft.set(null);
      this.selectBatch(this.selectedBatchId()!);
    });
  }

  cancelEdit() {
    this.editMap?.remove();
    this.editMap = null;
    this.editingDraft.set(null);
  }

  toggleSelect(jobId: number) {
    const s = new Set(this.selectedIds());
    if (s.has(jobId)) s.delete(jobId); else s.add(jobId);
    this.selectedIds.set(s);
  }

  selectAll() {
    const allIds = new Set<number>(this.drafts().map(d => d.jobId));
    this.selectedIds.set(allIds);
  }

  clearSelection() {
    this.selectedIds.set(new Set<number>());
  }

  bulkPublish() {
    const ids = [...this.selectedIds()];
    if (!ids.length) return;
    this.bulkUpdating.set(true);
    this.api.bulkUpdateDrafts({
      jobIds: ids,
      areaName: this.bulkAreaName || null,
      showInPortfolio: true,
      photoConsent: true
    }).subscribe({
      next: () => {
        this.bulkUpdating.set(false);
        this.clearSelection();
        this.bulkAreaName = '';
        this.selectBatch(this.selectedBatchId()!);
      },
      error: () => this.bulkUpdating.set(false)
    });
  }

  ngOnDestroy() {
    this.editMap?.remove();
  }

  deleteDraft(jobId: number) {
    if (!confirm('ลบรูปนี้ออก?')) return;
    this.api.deleteImportDraft(jobId).subscribe(() => {
      this.selectBatch(this.selectedBatchId()!);
    });
  }

  publishBatch() {
    const id = this.selectedBatchId();
    if (!id) return;
    const ready = this.drafts().filter(d => d.photoConsent).length;
    if (!confirm(`เผยแพร่ ${ready} รูปที่ตั้งค่า photoConsent=true ขึ้นพอร์ตโฟลิโอ?`)) return;
    this.publishing.set(true);
    this.api.publishImportBatch(id).subscribe({
      next: r => {
        this.publishing.set(false);
        alert(`เผยแพร่แล้ว ${r.published} รูป`);
        this.selectBatch(id);
        this.loadBatches();
      },
      error: () => {
        this.publishing.set(false);
        alert('เกิดข้อผิดพลาด');
      }
    });
  }

  materialLabel(m: Material) {
    return m === 'Stainless' ? 'สแตนเลส' : 'สังกะสี';
  }
}
