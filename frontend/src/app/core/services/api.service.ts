import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  GutterProduct, ServiceZone, ShopProfilePublic, EstimateResult,
  QuoteRequestSummary, QuoteRequestDetail, PricingConfig,
  ShopProfile, StatsResponse
} from '../models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  // Public
  getProducts() {
    return this.http.get<GutterProduct[]>(`${this.base}/api/products`);
  }
  getZones() {
    return this.http.get<ServiceZone[]>(`${this.base}/api/zones`);
  }
  getShopProfile() {
    return this.http.get<ShopProfilePublic>(`${this.base}/api/shop-profile`);
  }
  estimate(body: object) {
    return this.http.post<EstimateResult>(`${this.base}/api/estimate`, body);
  }
  createQuoteRequest(body: object) {
    return this.http.post<{ quoteNumber: string; quoteRequestId: number }>(`${this.base}/api/quote-requests`, body);
  }
  getQuotePdfUrl(id: number) {
    return `${this.base}/api/quote-requests/${id}/pdf`;
  }

  // Admin
  login(username: string, password: string) {
    return this.http.post<{ token: string }>(`${this.base}/api/admin/login`, { username, password });
  }
  getStats() {
    return this.http.get<StatsResponse>(`${this.base}/api/admin/stats`);
  }
  getQuoteRequests(params?: { status?: string; from?: string; to?: string; page?: number; pageSize?: number }) {
    let p = new HttpParams();
    if (params?.status) p = p.set('status', params.status);
    if (params?.from) p = p.set('from', params.from);
    if (params?.to) p = p.set('to', params.to);
    if (params?.page) p = p.set('page', params.page);
    if (params?.pageSize) p = p.set('pageSize', params.pageSize);
    return this.http.get<{ total: number; page: number; pageSize: number; items: QuoteRequestSummary[] }>(
      `${this.base}/api/admin/quote-requests`, { params: p });
  }
  getQuoteRequestDetail(id: number) {
    return this.http.get<QuoteRequestDetail>(`${this.base}/api/admin/quote-requests/${id}`);
  }
  updateQuoteStatus(id: number, status: string) {
    return this.http.put(`${this.base}/api/admin/quote-requests/${id}/status`, { status });
  }
  getAdminQuotePdfUrl(id: number) {
    return `${this.base}/api/admin/quote-requests/${id}/pdf`;
  }

  getAdminProducts() {
    return this.http.get<GutterProduct[]>(`${this.base}/api/admin/products`);
  }
  createProduct(body: object) {
    return this.http.post<GutterProduct>(`${this.base}/api/admin/products`, body);
  }
  updateProduct(id: number, body: object) {
    return this.http.put<GutterProduct>(`${this.base}/api/admin/products/${id}`, body);
  }
  deleteProduct(id: number) {
    return this.http.delete(`${this.base}/api/admin/products/${id}`);
  }

  getConfig() {
    return this.http.get<PricingConfig>(`${this.base}/api/admin/config`);
  }
  updateConfig(body: object) {
    return this.http.put<PricingConfig>(`${this.base}/api/admin/config`, body);
  }

  getAdminZones() {
    return this.http.get<ServiceZone[]>(`${this.base}/api/admin/zones`);
  }
  createZone(body: object) {
    return this.http.post<ServiceZone>(`${this.base}/api/admin/zones`, body);
  }
  updateZone(id: number, body: object) {
    return this.http.put<ServiceZone>(`${this.base}/api/admin/zones/${id}`, body);
  }
  deleteZone(id: number) {
    return this.http.delete(`${this.base}/api/admin/zones/${id}`);
  }

  getAdminShopProfile() {
    return this.http.get<ShopProfile>(`${this.base}/api/admin/shop-profile`);
  }
  updateShopProfile(body: object) {
    return this.http.put<ShopProfile>(`${this.base}/api/admin/shop-profile`, body);
  }
}
