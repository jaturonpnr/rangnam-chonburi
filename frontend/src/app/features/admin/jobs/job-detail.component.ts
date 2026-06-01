import { Component, OnInit, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { JobDetail, ServiceRequestStatus } from '../../../core/models';

@Component({
  selector: 'app-job-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, DatePipe],
  templateUrl: './job-detail.component.html'
})
export class JobDetailComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  job = signal<JobDetail | null>(null);
  activeTab = signal<'details' | 'photos' | 'claims'>('details');

  editForm = { warrantyMonths: 12, installedDate: '', areaName: '', lat: '', lng: '', showInPortfolio: false, photoConsent: false };
  saving = signal(false);

  uploadFile: File | null = null;
  uploadType = 'After';
  uploadCaption = '';
  uploading = signal(false);
  uploadError = signal('');

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.loadJob(id);
  }

  loadJob(id?: number) {
    const jobId = id ?? this.job()!.id;
    this.api.getAdminJobDetail(jobId).subscribe(j => {
      this.job.set(j);
      this.editForm = {
        warrantyMonths: j.warrantyMonths,
        installedDate: j.installedDate,
        areaName: j.areaName ?? '',
        lat: j.lat?.toString() ?? '',
        lng: j.lng?.toString() ?? '',
        showInPortfolio: j.showInPortfolio,
        photoConsent: j.photoConsent
      };
    });
  }

  saveJob() {
    const j = this.job();
    if (!j) return;
    this.saving.set(true);
    this.api.updateAdminJob(j.id, {
      warrantyMonths: this.editForm.warrantyMonths,
      installedDate: this.editForm.installedDate,
      areaName: this.editForm.areaName || null,
      lat: this.editForm.lat ? +this.editForm.lat : null,
      lng: this.editForm.lng ? +this.editForm.lng : null,
      showInPortfolio: this.editForm.showInPortfolio,
      photoConsent: this.editForm.photoConsent
    }).subscribe({
      next: () => { this.loadJob(); this.saving.set(false); },
      error: () => this.saving.set(false)
    });
  }

  onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.uploadFile = input.files?.[0] ?? null;
  }

  uploadPhoto() {
    if (!this.uploadFile || !this.job()) return;
    this.uploading.set(true);
    this.uploadError.set('');
    this.api.uploadJobPhoto(this.job()!.id, this.uploadFile, this.uploadType, this.uploadCaption || undefined)
      .subscribe({
        next: () => { this.uploadFile = null; this.uploadCaption = ''; this.loadJob(); this.uploading.set(false); },
        error: () => { this.uploadError.set('อัปโหลดไม่สำเร็จ'); this.uploading.set(false); }
      });
  }

  deletePhoto(photoId: number) {
    if (!confirm('ลบรูปภาพนี้?')) return;
    this.api.deleteJobPhoto(this.job()!.id, photoId).subscribe(() => this.loadJob());
  }

  updateClaimStatus(id: number, status: ServiceRequestStatus) {
    this.api.updateServiceRequestStatus(id, status).subscribe(() => this.loadJob());
  }

  downloadQr() {
    this.api.downloadJobQr(this.job()!.id).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = `${this.job()!.warrantyNumber}-qr.png`;
      a.click(); URL.revokeObjectURL(url);
    });
  }
  downloadPdf() {
    this.api.downloadJobWarrantyPdf(this.job()!.id).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = `${this.job()!.warrantyNumber}-warranty.pdf`;
      a.click(); URL.revokeObjectURL(url);
    });
  }
  materialLabel(m: string) { return m === 'Galvanized' ? 'สังกะสี' : 'สแตนเลส'; }
  logout() { this.auth.logout(); this.router.navigate(['/admin/login']); }
}
