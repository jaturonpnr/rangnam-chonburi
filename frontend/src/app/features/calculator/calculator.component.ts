import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { GutterProduct, ServiceZone, EstimateResult, ShopProfilePublic } from '../../core/models';

@Component({
  selector: 'app-calculator',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './calculator.component.html'
})
export class CalculatorComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  products = signal<GutterProduct[]>([]);
  zones = signal<ServiceZone[]>([]);
  shopProfile = signal<ShopProfilePublic | null>(null);
  estimate = signal<EstimateResult | null>(null);
  loading = signal(false);
  submitLoading = signal(false);
  showContactForm = signal(false);
  serverError = signal('');

  calcForm = this.fb.group({
    material: ['Galvanized', Validators.required],
    sizeInches: [null as number | null, Validators.required],
    finish: [null as string | null],
    lengthMeters: [null as number | null, [Validators.required, Validators.min(0.1)]],
    downspoutCount: [0, [Validators.required, Validators.min(0)]],
    floors: [1, [Validators.required, Validators.min(1)]],
    removeOld: [false],
    serviceZoneId: [null as number | null]
  });

  contactForm = this.fb.group({
    customerName: ['', [Validators.required, Validators.minLength(2)]],
    phone: ['', [Validators.required, Validators.pattern(/^\d{9,10}$/)]],
    address: ['']
  });

  // Convert reactive form values → signals so computed() can depend on them
  private formValue = toSignal(this.calcForm.valueChanges, { initialValue: this.calcForm.value });

  availableSizes = computed(() => {
    const mat = this.formValue().material;
    return [...new Set(this.products().filter(p => p.material === mat).map(p => p.sizeInches))].sort();
  });

  availableFinishes = computed(() => {
    const v = this.formValue();
    const mat = v.material;
    const size = +(v.sizeInches ?? 0);
    return this.products()
      .filter(p => p.material === mat && p.sizeInches === size && p.finish != null)
      .map(p => p.finish!);
  });

  isStainless = computed(() => this.formValue().material === 'Stainless');

  ngOnInit() {
    this.api.getProducts().subscribe(p => this.products.set(p));
    this.api.getZones().subscribe(z => this.zones.set(z));
    this.api.getShopProfile().subscribe(s => this.shopProfile.set(s));

    this.calcForm.get('material')?.valueChanges.subscribe(() => {
      this.calcForm.patchValue({ sizeInches: null, finish: null });
      this.estimate.set(null);
    });
    this.calcForm.get('sizeInches')?.valueChanges.subscribe(() => {
      this.calcForm.patchValue({ finish: null });
      this.estimate.set(null);
    });
  }

  private coerceForm() {
    const v = this.calcForm.value;
    return {
      ...v,
      sizeInches: v.sizeInches != null ? +v.sizeInches : v.sizeInches,
      lengthMeters: v.lengthMeters != null ? +v.lengthMeters : v.lengthMeters,
      downspoutCount: v.downspoutCount != null ? +v.downspoutCount : v.downspoutCount,
      floors: v.floors != null ? +v.floors : v.floors,
      serviceZoneId: v.serviceZoneId != null ? +v.serviceZoneId : null
    };
  }

  calculate() {
    if (this.calcForm.invalid) { this.calcForm.markAllAsTouched(); return; }
    this.loading.set(true);
    this.estimate.set(null);
    this.api.estimate(this.coerceForm()).subscribe({
      next: r => { this.estimate.set(r); this.loading.set(false); },
      error: e => { this.serverError.set(e.error?.error ?? 'เกิดข้อผิดพลาด'); this.loading.set(false); }
    });
  }

  submitQuote() {
    if (this.contactForm.invalid) { this.contactForm.markAllAsTouched(); return; }
    this.submitLoading.set(true);
    const body = { ...this.coerceForm(), ...this.contactForm.value };
    this.api.createQuoteRequest(body).subscribe({
      next: r => this.router.navigate(['/thank-you', r.quoteNumber], { state: { quoteRequestId: r.quoteRequestId } }),
      error: e => { this.serverError.set(e.error?.error ?? 'เกิดข้อผิดพลาด'); this.submitLoading.set(false); }
    });
  }

  formatNumber(n: number) {
    return n.toLocaleString('th-TH');
  }
}
