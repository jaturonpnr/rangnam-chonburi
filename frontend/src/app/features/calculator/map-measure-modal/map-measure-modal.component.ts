import { Component, Output, EventEmitter, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import '@geoman-io/leaflet-geoman-free';
import length from '@turf/length';
import { multiLineString } from '@turf/helpers';
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
  private map!: L.Map;
  private drawnLayers: L.Polyline[] = [];
  private locationMarker: L.CircleMarker | null = null;

  ngAfterViewInit() {
    // Fix Leaflet default icon paths broken by Angular bundler
    const iconDefault = L.icon({
      iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
      iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
      shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
      iconSize: [25, 41], iconAnchor: [12, 41]
    });
    L.Marker.prototype.options.icon = iconDefault;

    this.map = L.map('map-measure-container', { center: [13.756, 100.502], zoom: 15 });

    L.tileLayer(environment.satelliteTileUrl, {
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
      const layer = e.layer as L.Polyline;
      this.drawnLayers.push(layer);
      this.recalculate();
      layer.on('pm:edit', () => this.recalculate());
    });

    this.map.on('pm:remove', (e: any) => {
      this.drawnLayers = this.drawnLayers.filter(l => l !== e.layer);
      this.recalculate();
    });

    this.map.on('pm:drawstart', () => this.locationMarker?.setStyle({ opacity: 0, fillOpacity: 0 }));
    this.map.on('pm:drawend', () => this.locationMarker?.setStyle({ opacity: 1, fillOpacity: 1 }));
  }

  ngOnDestroy() {
    this.map?.remove();
  }

  private recalculate() {
    if (this.drawnLayers.length === 0) { this.totalMeters = 0; return; }
    const coords = this.drawnLayers.map(l =>
      (l.getLatLngs() as L.LatLng[]).map(ll => [ll.lng, ll.lat] as [number, number])
    );
    const km = length(multiLineString(coords), { units: 'kilometers' });
    this.totalMeters = Math.round(km * 1000 * 10) / 10;
  }

  locateMe() {
    navigator.geolocation.getCurrentPosition(
      pos => {
        const latlng: L.LatLngTuple = [pos.coords.latitude, pos.coords.longitude];
        this.map.setView(latlng, 18);
        this.locationMarker?.remove();
        this.locationMarker = L.circleMarker(latlng, {
          radius: 8, color: '#fff', weight: 2,
          fillColor: '#0284C7', fillOpacity: 1
        }).addTo(this.map).bindPopup('ตำแหน่งของคุณ').openPopup();
      },
      () => {}
    );
  }

  clearAll() {
    (this.map as any).pm.getGeomanLayers().forEach((l: L.Layer) => l.remove());
    this.drawnLayers = [];
    this.totalMeters = 0;
    this.locationMarker?.remove();
    this.locationMarker = null;
  }

  apply() {
    const center = this.map.getCenter();
    const coords = this.drawnLayers.map(l =>
      (l.getLatLngs() as L.LatLng[]).map(ll => [ll.lng, ll.lat] as [number, number])
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
