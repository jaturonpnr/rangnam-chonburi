import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { GutterProduct, BuildingType, ServiceZone, PricingConfig, ShopProfile } from '../../../core/models';

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './pricing.component.html'
})
export class PricingComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  activeTab = signal<'products' | 'buildings' | 'config' | 'zones' | 'shop'>('products');

  products = signal<GutterProduct[]>([]);
  buildings = signal<BuildingType[]>([]);
  zones = signal<ServiceZone[]>([]);
  config = signal<PricingConfig | null>(null);
  shop = signal<ShopProfile | null>(null);

  editingProductId = signal<number | null>(null);

  productForm = this.fb.group({
    material: ['Galvanized', Validators.required],
    sizeInches: [4, Validators.required],
    pricePerMeter: [0, [Validators.required, Validators.min(1)]],
    isActive: [true]
  });

  editingBuildingId = signal<number | null>(null);

  buildingForm = this.fb.group({
    label: ['', Validators.required],
    sizeInches: [6, Validators.required],
    displayOrder: [1, Validators.required],
    isActive: [true]
  });

  configForm = this.fb.group({
    minimumMeters: [10, Validators.required],
    downspoutPricePerPoint: [500, Validators.required],
    heightSurchargePercent: [20, Validators.required],
    removalPricePerMeter: [60, Validators.required],
    surveyFee: [1000, Validators.required]
  });

  zoneForm = this.fb.group({
    name: ['', Validators.required],
    travelSurcharge: [0, Validators.required],
    isActive: [true]
  });
  editingZoneId = signal<number | null>(null);

  shopForm = this.fb.group({
    shopName: ['', Validators.required],
    phone: ['', Validators.required],
    address: ['', Validators.required],
    logoUrl: [null as string | null],
    lineOaLink: [''],
    quoteValidityDays: [30, Validators.required],
    quoteFooterNote: ['']
  });

  saving = signal(false);
  savedMsg = signal('');

  ngOnInit() {
    this.loadAll();
  }

  loadAll() {
    this.api.getAdminProducts().subscribe(p => this.products.set(p));
    this.api.getAdminBuildingTypes().subscribe(bt => this.buildings.set(bt));
    this.api.getAdminZones().subscribe(z => this.zones.set(z));
    this.api.getConfig().subscribe(c => { this.config.set(c); this.configForm.patchValue(c as any); });
    this.api.getAdminShopProfile().subscribe(s => { this.shop.set(s); this.shopForm.patchValue(s as any); });
  }

  editProduct(p: GutterProduct) {
    this.editingProductId.set(p.id);
    this.productForm.patchValue({ material: p.material, sizeInches: p.sizeInches, pricePerMeter: p.pricePerMeter, isActive: p.isActive });
  }

  saveProduct() {
    if (this.productForm.invalid) return;
    const body = this.productForm.value;
    const id = this.editingProductId();
    const obs = id ? this.api.updateProduct(id, body) : this.api.createProduct(body);
    obs.subscribe(() => { this.api.getAdminProducts().subscribe(p => this.products.set(p)); this.resetProductForm(); this.flashSaved(); });
  }

  deleteProduct(id: number) {
    if (!confirm('ลบสินค้านี้?')) return;
    this.api.deleteProduct(id).subscribe(() => this.api.getAdminProducts().subscribe(p => this.products.set(p)));
  }

  resetProductForm() { this.editingProductId.set(null); this.productForm.reset({ material: 'Galvanized', sizeInches: 4, pricePerMeter: 0, isActive: true }); }

  editBuilding(b: BuildingType) {
    this.editingBuildingId.set(b.id);
    this.buildingForm.patchValue({ label: b.label, sizeInches: b.sizeInches, displayOrder: b.displayOrder, isActive: b.isActive });
  }

  saveBuilding() {
    if (this.buildingForm.invalid) return;
    const body = this.buildingForm.value;
    const id = this.editingBuildingId();
    const obs = id ? this.api.updateBuildingType(id, body) : this.api.createBuildingType(body);
    obs.subscribe(() => { this.api.getAdminBuildingTypes().subscribe(bt => this.buildings.set(bt)); this.resetBuildingForm(); this.flashSaved(); });
  }

  deleteBuilding(id: number) {
    if (!confirm('ลบประเภทอาคารนี้?')) return;
    this.api.deleteBuildingType(id).subscribe(() => this.api.getAdminBuildingTypes().subscribe(bt => this.buildings.set(bt)));
  }

  resetBuildingForm() { this.editingBuildingId.set(null); this.buildingForm.reset({ label: '', sizeInches: 6, displayOrder: 1, isActive: true }); }

  saveConfig() {
    if (this.configForm.invalid) return;
    this.api.updateConfig(this.configForm.value).subscribe(() => this.flashSaved());
  }

  editZone(z: ServiceZone) { this.editingZoneId.set(z.id); this.zoneForm.patchValue(z as any); }

  saveZone() {
    if (this.zoneForm.invalid) return;
    const id = this.editingZoneId();
    const obs = id ? this.api.updateZone(id, this.zoneForm.value) : this.api.createZone(this.zoneForm.value);
    obs.subscribe(() => { this.api.getAdminZones().subscribe(z => this.zones.set(z)); this.resetZoneForm(); this.flashSaved(); });
  }

  deleteZone(id: number) {
    if (!confirm('ลบโซนนี้?')) return;
    this.api.deleteZone(id).subscribe(() => this.api.getAdminZones().subscribe(z => this.zones.set(z)));
  }

  resetZoneForm() { this.editingZoneId.set(null); this.zoneForm.reset({ name: '', travelSurcharge: 0, isActive: true }); }

  saveShop() {
    if (this.shopForm.invalid) return;
    this.api.updateShopProfile(this.shopForm.value).subscribe(() => this.flashSaved());
  }

  flashSaved() { this.savedMsg.set('บันทึกเรียบร้อยแล้ว ✓'); setTimeout(() => this.savedMsg.set(''), 2500); }
  logout() { this.auth.logout(); this.router.navigate(['/admin/login']); }
  materialLabel(m: string) { return m === 'Galvanized' ? 'สังกะสี' : 'สแตนเลส'; }
}
