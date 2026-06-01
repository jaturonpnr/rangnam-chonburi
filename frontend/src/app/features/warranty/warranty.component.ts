import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { WarrantyCard, JobPhoto, ServiceRequestType } from '../../core/models';

@Component({
  selector: 'app-warranty',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './warranty.component.html'
})
export class WarrantyComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);

  card = signal<WarrantyCard | null>(null);
  loading = signal(true);
  notFound = signal(false);

  claimPhone = '';
  claimNote = '';
  claimType: ServiceRequestType = 'WarrantyClaim';
  submitting = signal(false);
  submitted = signal(false);
  submitError = signal('');

  daysLeft = computed(() => {
    const c = this.card();
    if (!c) return 0;
    const diff = new Date(c.warrantyExpiry).getTime() - Date.now();
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  });

  ngOnInit() {
    const token = this.route.snapshot.paramMap.get('token')!;
    this.api.getWarranty(token).subscribe({
      next: c => { this.card.set(c); this.loading.set(false); },
      error: () => { this.notFound.set(true); this.loading.set(false); }
    });
  }

  materialLabel(m: string) { return m === 'Galvanized' ? 'สังกะสี' : 'สแตนเลส'; }

  beforePhotos(): JobPhoto[] { return this.card()?.photos.filter(p => p.type === 'Before') ?? []; }
  afterPhotos(): JobPhoto[] { return this.card()?.photos.filter(p => p.type === 'After') ?? []; }

  submitClaim() {
    if (!this.claimPhone.trim()) return;
    const token = this.route.snapshot.paramMap.get('token')!;
    this.submitting.set(true);
    this.submitError.set('');
    this.api.createServiceRequest(token, {
      contactPhone: this.claimPhone,
      customerNote: this.claimNote || undefined,
      type: this.claimType
    }).subscribe({
      next: () => { this.submitted.set(true); this.submitting.set(false); },
      error: () => { this.submitError.set('เกิดข้อผิดพลาด กรุณาลองใหม่อีกครั้ง'); this.submitting.set(false); }
    });
  }
}
