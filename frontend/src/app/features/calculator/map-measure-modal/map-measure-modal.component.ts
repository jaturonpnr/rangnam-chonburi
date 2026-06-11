import { Component, Output, EventEmitter, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import type * as L from 'leaflet';
import { environment } from '../../../../environments/environment';
import { MapMeasureResult } from '../../../core/models';

@Component({
  selector: 'app-map-measure-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './map-measure-modal.component.html',
  styleUrls: ['./map-measure-modal.component.css']
})
export class MapMeasureModalComponent implements AfterViewInit, OnDestroy {
  @Output() applied = new EventEmitter<MapMeasureResult>();
  @Output() dismissed = new EventEmitter<void>();

  totalMeters = 0;
  private map: any = null;
  private drawnLayers: any[] = [];
  private locationMarker: any = null;
  private mapTourObj: any = null;

  async ngAfterViewInit() {
    const Lm = await import('leaflet').then(m => (m as any).default ?? m);
    await import('@geoman-io/leaflet-geoman-free');

    const iconDefault = Lm.icon({
      iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
      iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
      shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
      iconSize: [25, 41], iconAnchor: [12, 41]
    });
    Lm.Marker.prototype.options.icon = iconDefault;

    this.map = Lm.map('map-measure-container', { center: [13.756, 100.502] as [number, number], zoom: 15 });

    Lm.tileLayer(environment.satelliteTileUrl, {
      attribution: environment.satelliteAttribution,
      maxZoom: 20,
      maxNativeZoom: 18
    }).addTo(this.map);

    (this.map as any).pm.addControls({
      drawPolyline: true,
      drawMarker: false,
      drawCircleMarker: false,
      drawPolygon: false,
      drawCircle: false,
      drawRectangle: false,
      drawText: false,
      editMode: true,
      dragMode: false,
      cutPolygon: false,
      removalMode: true,
    });

    this.map.on('pm:create', (e: any) => {
      this.drawnLayers.push(e.layer);
      this.recalculate();
      e.layer.on('pm:edit', () => this.recalculate());
    });

    this.map.on('pm:remove', (e: any) => {
      this.drawnLayers = this.drawnLayers.filter((l: any) => l !== e.layer);
      this.recalculate();
    });

    this.map.on('pm:drawstart', () => this.locationMarker?.setStyle({ opacity: 0, fillOpacity: 0 }));
    this.map.on('pm:drawend', () => this.locationMarker?.setStyle({ opacity: 1, fillOpacity: 1 }));

    if (!localStorage.getItem('map_tour_seen')) {
      setTimeout(() => this.startMapTour(), 600);
    }
  }

  ngOnDestroy() {
    this.mapTourObj?.destroy();
    this.map?.remove();
  }

  async startMapTour() {
    this.mapTourObj?.destroy();
    const { driver } = await import('driver.js');

    this.mapTourObj = driver({
      showProgress: true,
      overlayOpacity: 0,
      nextBtnText: 'ถัดไป →',
      prevBtnText: '← ย้อนกลับ',
      doneBtnText: 'เข้าใจแล้ว ✓',
      progressText: '{{current}} / {{total}}',
      onDestroyed: () => {
        localStorage.setItem('map_tour_seen', '1');
      },
      steps: [
        {
          element: '#tour-locate-btn',
          popover: {
            title: 'ใช้ตำแหน่งปัจจุบัน',
            description: 'กดเพื่อให้แผนที่เลื่อนไปยังบ้านของคุณอัตโนมัติ',
            side: 'top', align: 'start',
          }
        },
        {
          element: '#map-measure-container',
          popover: {
            title: 'วาดเส้นบนแผนที่',
            description: 'กดปุ่ม <b>✏️ วาดเส้น</b> ในแถบเครื่องมือ แล้วคลิกตามขอบหลังคาทีละจุด — ต้องการหลายด้าน วาดเส้นใหม่ได้เลย ระบบบวกรวมให้',
            side: 'bottom', align: 'center',
          }
        },
        {
          element: '#tour-length-display',
          popover: {
            title: 'ความยาวรวม',
            description: 'ดูผลรวมความยาวตรงนี้ อัปเดตเรียลไทม์ขณะปักจุด',
            side: 'top', align: 'center',
          }
        },
        {
          element: '#tour-clear-btn',
          popover: {
            title: 'ล้าง / วาดใหม่',
            description: 'กดเพื่อลบเส้นทั้งหมดและเริ่มต้นใหม่',
            side: 'top', align: 'start',
          }
        },
        {
          element: '#tour-apply-btn',
          popover: {
            title: 'ใช้ค่านี้',
            description: 'พอใจแล้วกด "ใช้ค่านี้" — เมตรจะถูกเติมเข้าฟอร์มคำนวณอัตโนมัติ',
            side: 'top', align: 'end',
          }
        },
      ],
    });

    this.mapTourObj.drive();
  }

  private async recalculate() {
    if (this.drawnLayers.length === 0) { this.totalMeters = 0; return; }
    const [{ default: length }, { multiLineString }] = await Promise.all([
      import('@turf/length'),
      import('@turf/helpers')
    ]);
    const coords = this.drawnLayers.map((l: any) =>
      l.getLatLngs().map((ll: any) => [ll.lng, ll.lat] as [number, number])
    );
    const km = length(multiLineString(coords), { units: 'kilometers' });
    this.totalMeters = Math.round(km * 1000 * 10) / 10;
  }

  async locateMe() {
    if (!navigator.geolocation) return;
    const Lm = await import('leaflet').then(m => (m as any).default ?? m);
    navigator.geolocation.getCurrentPosition(
      pos => {
        const latlng: [number, number] = [pos.coords.latitude, pos.coords.longitude];
        this.map.setView(latlng, 18);
        this.locationMarker?.remove();
        this.locationMarker = Lm.circleMarker(latlng, {
          radius: 8, color: '#fff', weight: 2,
          fillColor: '#0284C7', fillOpacity: 1
        }).addTo(this.map);
      },
      () => {}
    );
  }

  clearAll() {
    (this.map as any)?.pm.getGeomanLayers().forEach((l: any) => l.remove());
    this.drawnLayers = [];
    this.totalMeters = 0;
    this.locationMarker?.remove();
    this.locationMarker = null;
  }

  apply() {
    const center = this.map.getCenter();
    const coords = this.drawnLayers.map((l: any) =>
      l.getLatLngs().map((ll: any) => [ll.lng, ll.lat] as [number, number])
    );
    this.applied.emit({
      geojson: { type: 'MultiLineString', coordinates: coords },
      measuredLengthMeters: this.totalMeters,
      centerLat: center.lat,
      centerLng: center.lng,
      zoom: this.map.getZoom()
    });
  }
}
