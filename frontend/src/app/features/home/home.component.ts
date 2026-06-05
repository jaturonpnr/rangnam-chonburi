import { Component, OnInit, OnDestroy, AfterViewInit, signal, inject, computed, NgZone } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { ShopProfilePublic, PortfolioPostPin } from '../../core/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {
  private api = inject(ApiService);
  private zone = inject(NgZone);

  shop = signal<ShopProfilePublic | null>(null);
  navScrolled = signal(false);
  portfolioSummary = signal<{ total: number; byArea: { area: string; count: number }[] } | null>(null);
  pins = signal<PortfolioPostPin[]>([]);

  totalInstalled = computed(() => this.portfolioSummary()?.total ?? 0);
  byArea = computed(() => this.portfolioSummary()?.byArea ?? []);

  // Geolocation
  geoState = signal<'idle'|'locating'|'success'|'denied'|'error'|'unsupported'>('idle');
  userLat = signal<number|null>(null);
  userLng = signal<number|null>(null);

  nearPinIds = computed(() => {
    const lat = this.userLat();
    const lng = this.userLng();
    if (lat === null || lng === null) return new Set<number>();
    return new Set(this.pins()
      .filter(p => this.haversineKm(lat, lng, p.approxLat, p.approxLng) <= environment.nearRadiusKm)
      .map(p => p.id));
  });

  nearCount = computed(() => this.nearPinIds().size);
  readonly nearRadiusKm = environment.nearRadiusKm;

  private scrollHandler = () => this.navScrolled.set(window.scrollY > 48);
  private portfolioMap: any = null;
  private mapMarkers: any[] = [];
  private userMarker: any = null;
  private userCircle: any = null;

  ngOnInit() {
    this.api.getShopProfile().subscribe(s => this.shop.set(s));
    window.addEventListener('scroll', this.scrollHandler, { passive: true });

    this.api.getPortfolioPostSummary().subscribe(s => this.portfolioSummary.set(s));
    this.api.getPortfolioPosts().subscribe({
      next: pins => { this.pins.set(pins); this.addMapMarkers(pins); },
      error: () => {}
    });
  }

  ngAfterViewInit() {
    // Init map immediately after DOM is ready — independent of API call
    setTimeout(() => this.initPortfolioMap(), 0);
  }

  ngOnDestroy() {
    window.removeEventListener('scroll', this.scrollHandler);
    this.userMarker?.remove();
    this.userCircle?.remove();
    this.portfolioMap?.remove();
  }

  formatPhone(phone: string): string {
    if (phone.length === 10) return `${phone.slice(0, 3)}-${phone.slice(3, 6)}-${phone.slice(6)}`;
    if (phone.length === 9) return `${phone.slice(0, 2)}-${phone.slice(2, 5)}-${phone.slice(5)}`;
    return phone;
  }

  private haversineKm(lat1: number, lng1: number, lat2: number, lng2: number): number {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLng = (lng2 - lng1) * Math.PI / 180;
    const a = Math.sin(dLat / 2) ** 2
      + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLng / 2) ** 2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  }

  requestGeo() {
    if (!navigator.geolocation) { this.geoState.set('unsupported'); return; }
    this.geoState.set('locating');
    navigator.geolocation.getCurrentPosition(
      pos => this.zone.run(() => {
        this.userLat.set(pos.coords.latitude);
        this.userLng.set(pos.coords.longitude);
        this.geoState.set('success');
        this.updateGeoMap();
      }),
      err => this.zone.run(() => {
        this.geoState.set(err.code === 1 ? 'denied' : 'error');
      }),
      { timeout: 10000, maximumAge: 60000 }
    );
  }

  resetGeo() {
    this.geoState.set('idle');
    this.userLat.set(null);
    this.userLng.set(null);
    this.userMarker?.remove(); this.userMarker = null;
    this.userCircle?.remove(); this.userCircle = null;
    import('leaflet').then(m => (m as any).default ?? m)
      .then(L => this.renderMapMarkers(L, this.pins()));
    this.portfolioMap?.setView([13.3435218, 100.9820816], 11);
  }

  private async updateGeoMap() {
    const lat = this.userLat();
    const lng = this.userLng();
    if (lat === null || lng === null || !this.portfolioMap) return;
    const L = await import('leaflet').then(m => (m as any).default ?? m);

    this.userMarker?.remove();
    this.userCircle?.remove();

    this.portfolioMap.setView([lat, lng], 12);

    this.userCircle = L.circle([lat, lng], {
      radius: environment.nearRadiusKm * 1000,
      color: '#38BDF8', fillColor: '#38BDF8', fillOpacity: 0.08,
      weight: 2, dashArray: '6 4'
    }).addTo(this.portfolioMap);

    this.userMarker = L.circleMarker([lat, lng], {
      radius: 10, fillColor: '#fff', color: '#38BDF8', weight: 3, fillOpacity: 1
    }).bindPopup('<b>ตำแหน่งของคุณ</b>').addTo(this.portfolioMap);

    this.renderMapMarkers(L, this.pins());
  }

  private async initPortfolioMap() {
    const L = await import('leaflet').then(m => (m as any).default ?? m);
    const el = document.getElementById('home-portfolio-map');
    if (!el || this.portfolioMap) return;

    this.portfolioMap = L.map(el, {
      center: [13.3435218, 100.9820816] as [number, number],
      zoom: 11,
      scrollWheelZoom: false,
      zoomControl: true
    });

    L.tileLayer(environment.satelliteTileUrl, {
      attribution: environment.satelliteAttribution,
      maxZoom: 20,
      maxNativeZoom: 18
    }).addTo(this.portfolioMap);
  }

  private async addMapMarkers(pins: PortfolioPostPin[]) {
    const L = await import('leaflet').then(m => (m as any).default ?? m);
    if (!this.portfolioMap) await this.initPortfolioMap();
    this.renderMapMarkers(L, pins);
  }

  private renderMapMarkers(L: any, pins: PortfolioPostPin[]) {
    if (!this.portfolioMap) return;
    this.mapMarkers.forEach(m => m.remove());
    this.mapMarkers = [];
    const nearIds = this.nearPinIds();
    for (const pin of pins) {
      const isNear = nearIds.size > 0 && nearIds.has(pin.id);
      const marker = L.circleMarker([pin.approxLat, pin.approxLng] as [number, number], {
        radius: isNear ? 10 : 7,
        color: '#0D2461', weight: isNear ? 3 : 2,
        fillColor: isNear ? '#f97316' : '#38BDF8',
        fillOpacity: isNear ? 1 : 0.9
      })
        .addTo(this.portfolioMap)
        .bindPopup(`<div style="font-family:sans-serif;font-size:13px;min-width:120px;">
          <strong>${pin.areaName ?? 'ชลบุรี'}</strong>
          ${pin.title ? `<br><span style="color:#555;">${pin.title}</span>` : ''}
        </div>`);
      this.mapMarkers.push(marker);
    }
  }
}
