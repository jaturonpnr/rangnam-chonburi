// frontend/src/app/features/portfolio/portfolio.component.ts
import { Component, OnInit, OnDestroy, signal, computed, inject, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ApiService } from '../../core/services/api.service';
import { PortfolioPostPin } from '../../core/models';

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

  pins = signal<PortfolioPostPin[]>([]);
  selectedArea = signal<string | null>(null);
  activePinId = signal<number | null>(null);
  activeEmbedUrl = signal<SafeResourceUrl | null>(null);
  embedLoaded = signal(false);
  loading = signal(true);

  private _pendingUrl: string | null = null;

  areas = computed(() => {
    const all = this.pins().map(p => p.areaName).filter((a): a is string => !!a);
    return [...new Set(all)].sort();
  });

  private map: any = null;
  private markers: any[] = [];

  private escHtml(s: string) {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  ngOnInit() {
    this.api.getPortfolioPosts().subscribe({
      next: pins => {
        this.pins.set(pins);
        this.loading.set(false);
        this.initMap(pins);
      },
      error: () => this.loading.set(false)
    });

    // Global bridge for Leaflet popup button → Angular (must run inside NgZone)
    (window as any).ppOpen = (id: number, url: string) =>
      this.zone.run(() => this.openPanel(id, url));
  }

  ngOnDestroy() {
    delete (window as any).ppOpen;
    this.map?.remove();
    this.map = null;
  }

  private async initMap(pins: PortfolioPostPin[]) {
    const L = await import('leaflet');
    if (this.map) { this.map.remove(); this.map = null; }

    this.map = L.map('portfolio-map', { zoomControl: true }).setView([13.36, 101.0], 10);
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

    filtered.forEach(pin => {
      const marker = L.circleMarker([pin.approxLat, pin.approxLng], {
        radius: 8, fillColor: '#38bdf8', color: '#0D1B3E',
        weight: 2, opacity: 1, fillOpacity: 0.85
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
    import('leaflet').then(L => this.renderMarkers(L, this.pins()));
  }

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
    // embedLoaded stays false until iframe fires (load) event
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
