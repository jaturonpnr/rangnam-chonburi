import { Component, OnInit, OnDestroy, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { PortfolioPin, PortfolioSummary } from '../../core/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './portfolio.component.html'
})
export class PortfolioComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);

  pins = signal<PortfolioPin[]>([]);
  summary = signal<PortfolioSummary | null>(null);
  areaLabel = computed(() => {
    const s = this.summary();
    if (!s || s.byArea.length === 0) return '';
    return s.byArea.map(a => a.name + ' ' + a.count + ' จุด').join(' / ');
  });
  private map: any = null;

  ngOnInit() {
    this.api.getPortfolioSummary().subscribe(s => this.summary.set(s));
    this.api.getPortfolioPins().subscribe(p => {
      this.pins.set(p);
      setTimeout(() => this.initMap(p), 150);
    });
  }

  ngOnDestroy() { this.map?.remove(); }

  materialLabel(m: string) { return m === 'Galvanized' ? 'สังกะสี' : 'สแตนเลส'; }

  private async initMap(pins: PortfolioPin[]) {
    const L = await import('leaflet');
    const el = document.getElementById('portfolio-map');
    if (!el || this.map) return;

    const defaultCenter: [number, number] = [13.16, 100.93];
    this.map = L.map(el, { center: defaultCenter, zoom: 11 });

    L.tileLayer(environment.satelliteTileUrl, {
      attribution: environment.satelliteAttribution,
      maxZoom: 20,
      maxNativeZoom: 18
    }).addTo(this.map);

    for (const pin of pins) {
      const photos = pin.consentedPhotos.slice(0, 3)
        .map(p => `<img src="${p.url}" style="width:80px;height:60px;object-fit:cover;border-radius:4px;margin:2px;" />`)
        .join('');

      const popup = `
        <div style="font-family:sans-serif; min-width:160px;">
          <div style="font-weight:600; margin-bottom:4px;">${pin.areaName ?? 'ไม่ระบุพื้นที่'}</div>
          <div style="font-size:12px; color:#555;">${this.materialLabel(pin.material)} — ${pin.installedDate}</div>
          ${photos ? `<div style="margin-top:6px; display:flex; flex-wrap:wrap; gap:2px;">${photos}</div>` : ''}
        </div>`;

      L.circleMarker([pin.approxLat, pin.approxLng], {
        radius: 8, color: '#0284c7', weight: 2,
        fillColor: '#38bdf8', fillOpacity: 0.85
      }).addTo(this.map).bindPopup(popup);
    }
  }
}
