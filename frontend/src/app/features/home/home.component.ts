import { Component, OnInit, OnDestroy, signal, inject, computed } from '@angular/core';
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
export class HomeComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);

  shop = signal<ShopProfilePublic | null>(null);
  navScrolled = signal(false);
  portfolioSummary = signal<{ total: number; byArea: { area: string; count: number }[] } | null>(null);

  totalInstalled = computed(() => this.portfolioSummary()?.total ?? 0);
  byArea = computed(() => this.portfolioSummary()?.byArea ?? []);

  private scrollHandler = () => this.navScrolled.set(window.scrollY > 48);
  private portfolioMap: any = null;

  ngOnInit() {
    this.api.getShopProfile().subscribe(s => this.shop.set(s));
    window.addEventListener('scroll', this.scrollHandler, { passive: true });

    this.api.getPortfolioPostSummary().subscribe(s => this.portfolioSummary.set(s));
    this.api.getPortfolioPosts().subscribe({
      next: pins => setTimeout(() => this.initPortfolioMap(pins), 200),
      error: () => {}
    });
  }

  ngOnDestroy() {
    window.removeEventListener('scroll', this.scrollHandler);
    this.portfolioMap?.remove();
  }

  formatPhone(phone: string): string {
    if (phone.length === 10) return `${phone.slice(0, 3)}-${phone.slice(3, 6)}-${phone.slice(6)}`;
    if (phone.length === 9) return `${phone.slice(0, 2)}-${phone.slice(2, 5)}-${phone.slice(5)}`;
    return phone;
  }

  private async initPortfolioMap(pins: PortfolioPostPin[]) {
    const L = await import('leaflet');
    const el = document.getElementById('home-portfolio-map');
    if (!el || this.portfolioMap) return;

    this.portfolioMap = L.map(el, {
      center: [13.16, 100.93] as [number, number],
      zoom: 11,
      scrollWheelZoom: false,
      zoomControl: true
    });

    L.tileLayer(environment.satelliteTileUrl, {
      attribution: environment.satelliteAttribution,
      maxZoom: 20,
      maxNativeZoom: 18
    }).addTo(this.portfolioMap);

    for (const pin of pins) {
      L.circleMarker([pin.approxLat, pin.approxLng] as [number, number], {
        radius: 7, color: '#0D2461', weight: 2,
        fillColor: '#38BDF8', fillOpacity: 0.9
      })
        .addTo(this.portfolioMap)
        .bindPopup(`<div style="font-family:sans-serif;font-size:13px;min-width:120px;">
          <strong>${pin.areaName ?? 'ชลบุรี'}</strong>
          ${pin.title ? `<br><span style="color:#555;">${pin.title}</span>` : ''}
        </div>`);
    }
  }
}
