import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { Renderer2 } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { BuildingType, ServiceZone, EstimateResult, ShopProfilePublic, MapMeasureResult } from '../../core/models';
import { MapMeasureModalComponent } from './map-measure-modal/map-measure-modal.component';

@Component({
  selector: 'app-calculator',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MapMeasureModalComponent],
  templateUrl: './calculator.component.html'
})
export class CalculatorComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private renderer = inject(Renderer2);

  buildingTypes = signal<BuildingType[]>([]);
  zones = signal<ServiceZone[]>([]);
  shopProfile = signal<ShopProfilePublic | null>(null);
  estimate = signal<EstimateResult | null>(null);
  loading = signal(false);
  submitLoading = signal(false);
  showContactForm = signal(false);
  showMapModal = signal(false);
  serverError = signal('');

  private measureSource: 'Manual' | 'Map' = 'Manual';
  private mapMeasureResult: MapMeasureResult | null = null;

  calcForm = this.fb.group({
    material: ['Galvanized', Validators.required],
    buildingTypeId: [null as number | null, Validators.required],
    lengthMeters: [null as number | null, [Validators.required, Validators.min(0.1)]],
    downspoutCount: [0, [Validators.required, Validators.min(0)]],
    floors: [1, [Validators.required, Validators.min(1)]],
    removeOld: [false],
    serviceZoneId: [null as number | null]
  });

  contactForm = this.fb.group({
    customerName: ['', [Validators.required, Validators.minLength(2)]],
    phone: ['', [Validators.required, Validators.pattern(/^\d{9,10}$/)]],
    address: [''],
    locationDetail: ['']
  });

  ngOnInit() {
    this.renderer.addClass(document.body, 'calc-theme');
    this.api.getBuildingTypes().subscribe(bt => this.buildingTypes.set(bt));
    this.api.getZones().subscribe(z => this.zones.set(z));
    this.api.getShopProfile().subscribe(s => this.shopProfile.set(s));
    this.calcForm.get('material')?.valueChanges.subscribe(() => { this.estimate.set(null); });
  }

  onLengthMetersInput() {
    if (this.measureSource === 'Map') {
      this.measureSource = 'Manual';
      this.mapMeasureResult = null;
    }
  }

  onMapApplied(result: MapMeasureResult) {
    this.mapMeasureResult = result;
    this.measureSource = 'Map';
    this.calcForm.patchValue({ lengthMeters: result.measuredLengthMeters });
    this.showMapModal.set(false);
  }

  private coerceForm() {
    const v = this.calcForm.value;
    return {
      ...v,
      buildingTypeId: v.buildingTypeId != null ? +v.buildingTypeId : null,
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
    const body = {
      ...this.coerceForm(),
      ...this.contactForm.value,
      measureSource: this.measureSource,
      measuredLengthMeters: this.mapMeasureResult?.measuredLengthMeters ?? null,
      measuredGeoJson: this.mapMeasureResult ? JSON.stringify(this.mapMeasureResult.geojson) : null,
      mapCenterLat: this.mapMeasureResult?.centerLat ?? null,
      mapCenterLng: this.mapMeasureResult?.centerLng ?? null,
      mapZoom: this.mapMeasureResult?.zoom ?? null,
    };
    this.api.createQuoteRequest(body).subscribe({
      next: r => this.router.navigate(['/thank-you', r.quoteNumber], { state: { quoteRequestId: r.quoteRequestId } }),
      error: e => { this.serverError.set(e.error?.error ?? 'เกิดข้อผิดพลาด'); this.submitLoading.set(false); }
    });
  }

  ngOnDestroy() {
    this.renderer.removeClass(document.body, 'calc-theme');
  }

  formatNumber(n: number) { return n.toLocaleString('th-TH'); }
  formatPhone(phone: string): string {
    if (phone.length === 10) return `${phone.slice(0, 3)}-${phone.slice(3, 6)}-${phone.slice(6)}`;
    if (phone.length === 9) return `${phone.slice(0, 2)}-${phone.slice(2, 5)}-${phone.slice(5)}`;
    return phone;
  }
}
