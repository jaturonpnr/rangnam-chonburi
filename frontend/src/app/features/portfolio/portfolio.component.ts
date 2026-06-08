import { Component, OnInit, OnDestroy, signal, computed, inject, NgZone, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ApiService } from '../../core/services/api.service';
import { MetaSeoService } from '../../core/services/meta-seo.service';
import { PortfolioPostPin, ShopProfilePublic } from '../../core/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './portfolio.component.html',
  styleUrls: ['./portfolio.component.css']
})
export class PortfolioComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private sanitizer = inject(DomSanitizer);
  private zone = inject(NgZone);
  private platformId = inject(PLATFORM_ID);
  private metaSeo = inject(MetaSeoService);

  pins = signal<PortfolioPostPin[]>([]);
  selectedArea = signal<string | null>(null);
  activePinId = signal<number | null>(null);
  activeEmbedUrl = signal<SafeResourceUrl | null>(null);
  embedLoaded = signal(false);
  loading = signal(true);

  // Geolocation
  geoState = signal<'idle'|'locating'|'success'|'denied'|'error'|'unsupported'>('idle');
  userLat = signal<number|null>(null);
  userLng = signal<number|null>(null);
  nearRadius = signal<number>(environment.nearRadiusKm);
  shop = signal<ShopProfilePublic|null>(null);

  nearPinIds = computed(() => {
    const lat = this.userLat();
    const lng = this.userLng();
    if (lat === null || lng === null) return new Set<number>();
    const r = this.nearRadius();
    return new Set(this.pins()
      .filter(p => this.haversineKm(lat, lng, p.approxLat, p.approxLng) <= r)
      .map(p => p.id));
  });

  nearCount = computed(() => this.nearPinIds().size);

  areas = computed(() => {
    const all = this.pins().map(p => p.areaName).filter((a): a is string => !!a);
    return [...new Set(all)].sort();
  });

  private map: any = null;
  private markers: any[] = [];
  private userMarker: any = null;
  private userCircle: any = null;
  private _pendingUrl: string | null = null;

  private escHtml(s: string) {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  private haversineKm(lat1: number, lng1: number, lat2: number, lng2: number): number {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLng = (lng2 - lng1) * Math.PI / 180;
    const a = Math.sin(dLat / 2) ** 2
      + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLng / 2) ** 2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  }

  ngOnInit() {
    this.metaSeo.set({
      title: 'ผลงานติดตั้งรางน้ำฝน ชลบุรี — ส.จาตุรนต์ รางน้ำ',
      description: 'ดูผลงานติดตั้งรางน้ำฝนจริงทั่วชลบุรี ศรีราชา พัทยา กว่า 500 หลัง ดูโพสต์ Facebook ประกอบ',
      canonical: 'https://rangnam-chonburi.vercel.app/portfolio'
    });
    this.api.getPortfolioPosts().subscribe({
      next: pins => {
        this.pins.set(pins);
        this.loading.set(false);
        if (isPlatformBrowser(this.platformId)) {
          this.initMap(pins);
        }
      },
      error: () => this.loading.set(false)
    });

    this.api.getShopProfile().subscribe(s => this.shop.set(s));

    if (isPlatformBrowser(this.platformId)) {
      (window as any).ppOpen = (id: number, url: string) =>
        this.zone.run(() => this.openPanel(id, url));
    }
  }

  ngOnDestroy() {
    if (isPlatformBrowser(this.platformId)) {
      delete (window as any).ppOpen;
    }
    this.map?.remove();
    this.map = null;
    this.userMarker?.remove();
    this.userCircle?.remove();
  }

  private async initMap(pins: PortfolioPostPin[]) {
    const L = await import('leaflet').then(m => (m as any).default ?? m);
    if (this.map) { this.map.remove(); this.map = null; }

    this.map = L.map('portfolio-map', { zoomControl: true }).setView([13.3435218, 100.9820816], 10);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    this.renderMarkers(L, pins);
  }

  private renderMarkers(L: any, pins: PortfolioPostPin[]) {
    this.markers.forEach(m => m.remove());
    this.markers = [];
    const area = this.selectedArea();
    const filtered = area ? pins.filter(p => p.areaName === area) : pins;
    const nearIds = this.nearPinIds();

    filtered.forEach(pin => {
      const isNear = nearIds.size > 0 && nearIds.has(pin.id);
      const marker = L.circleMarker([pin.approxLat, pin.approxLng], {
        radius: isNear ? 10 : 8,
        fillColor: isNear ? '#f97316' : '#38bdf8',
        color: '#0D1B3E',
        weight: isNear ? 3 : 2,
        opacity: 1,
        fillOpacity: isNear ? 1 : 0.85
      });
      marker.bindPopup(`
        <div style="font-family:sans-serif;font-size:13px;min-width:140px">
          <b>${this.escHtml(pin.areaName ?? 'ผลงาน')}</b><br>
          ${pin.title ? `<span style="color:#555">${this.escHtml(pin.title)}</span><br>` : ''}
          ${pin.postedDate ? `<span style="color:#888;font-size:11px">${pin.postedDate.substring(0, 10)}</span><br>` : ''}
          <button onclick="window.ppOpen(${pin.id},'${encodeURIComponent(pin.fbPostUrl)}')"
            style="margin-top:6px;padding:4px 10px;background:#38bdf8;border:none;border-radius:4px;cursor:pointer;font-size:12px;color:#0D1B3E;font-weight:600">
            ดูโพสต์ Facebook
          </button>
        </div>
      `);
      marker.addTo(this.map);
      this.markers.push(marker);
    });
  }

  filterArea(area: string | null) {
    this.selectedArea.set(area);
    import('leaflet').then(m => (m as any).default ?? m).then(L => this.renderMarkers(L, this.pins()));
  }

  // --- Geolocation ---

  requestGeo() {
    if (!isPlatformBrowser(this.platformId) || !navigator.geolocation) { this.geoState.set('unsupported'); return; }
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

  setRadius(r: number) {
    this.nearRadius.set(r);
    if (this.geoState() === 'success') this.updateGeoMap();
  }

  resetGeo() {
    this.geoState.set('idle');
    this.userLat.set(null);
    this.userLng.set(null);
    this.userMarker?.remove(); this.userMarker = null;
    this.userCircle?.remove(); this.userCircle = null;
    import('leaflet').then(m => (m as any).default ?? m)
      .then(L => this.renderMarkers(L, this.pins()));
    this.map?.setView([13.3435218, 100.9820816], 10);
  }

  private async updateGeoMap() {
    const lat = this.userLat();
    const lng = this.userLng();
    if (lat === null || lng === null || !this.map) return;
    const L = await import('leaflet').then(m => (m as any).default ?? m);

    this.userMarker?.remove();
    this.userCircle?.remove();

    this.map.setView([lat, lng], 12);

    this.userCircle = L.circle([lat, lng], {
      radius: this.nearRadius() * 1000,
      color: '#38bdf8', fillColor: '#38bdf8', fillOpacity: 0.08,
      weight: 2, dashArray: '6 4'
    }).addTo(this.map);

    this.userMarker = L.circleMarker([lat, lng], {
      radius: 10, fillColor: '#fff', color: '#38bdf8', weight: 3, fillOpacity: 1
    }).bindPopup('<b>ตำแหน่งของคุณ</b>').addTo(this.map);

    this.renderMarkers(L, this.pins());
  }

  // --- FB embed panel ---

  openPanel(id: number, encodedUrl: string) {
    this.map?.closePopup();
    this.activePinId.set(id);
    this.activeEmbedUrl.set(null);
    this.embedLoaded.set(false);
    this._pendingUrl = decodeURIComponent(encodedUrl);
    this.loadEmbed();
  }

  loadEmbed() {
    const url = this._pendingUrl;
    if (!url) return;
    const embedSrc = `https://www.facebook.com/plugins/post.php?href=${encodeURIComponent(url)}&show_text=true&width=500`;
    this.activeEmbedUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(embedSrc));
  }

  onIframeLoad() {
    this.embedLoaded.set(true);
  }

  closePanel() {
    this.activePinId.set(null);
    this.activeEmbedUrl.set(null);
    this.embedLoaded.set(false);
    this._pendingUrl = null;
  }
}
