import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  GutterProduct, BuildingType, ServiceZone, ShopProfilePublic, EstimateResult,
  QuoteRequestSummary, QuoteRequestDetail, PricingConfig,
  ShopProfile, StatsResponse, MeasureSource,
  JobSummary, JobDetail, JobPhoto, WarrantyCard, PortfolioPin, PortfolioSummary,
  AdminServiceRequest, ServiceRequestStatus
} from '../models';

export interface CreateQuoteRequestPayload {
  [key: string]: unknown;
  measureSource?: MeasureSource;
  measuredLengthMeters?: number | null;
  measuredGeoJson?: string | null;
  mapCenterLat?: number | null;
  mapCenterLng?: number | null;
  mapZoom?: number | null;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  // Public
  getProducts() {
    return this.http.get<GutterProduct[]>(`${this.base}/api/products`);
  }
  getBuildingTypes() {
    return this.http.get<BuildingType[]>(`${this.base}/api/building-types`);
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
  createQuoteRequest(body: CreateQuoteRequestPayload) {
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

  getAdminBuildingTypes() {
    return this.http.get<BuildingType[]>(`${this.base}/api/admin/building-types`);
  }
  createBuildingType(body: object) {
    return this.http.post<BuildingType>(`${this.base}/api/admin/building-types`, body);
  }
  updateBuildingType(id: number, body: object) {
    return this.http.put<BuildingType>(`${this.base}/api/admin/building-types/${id}`, body);
  }
  deleteBuildingType(id: number) {
    return this.http.delete(`${this.base}/api/admin/building-types/${id}`);
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

  // ── Public Warranty + Portfolio ───────────────────────────────────────────
  getWarranty(token: string) {
    return this.http.get<WarrantyCard>(`${this.base}/api/warranty/${token}`);
  }
  createServiceRequest(token: string, body: { contactPhone: string; customerNote?: string; type: string }) {
    return this.http.post<{ id: number }>(`${this.base}/api/warranty/${token}/service-request`, body);
  }
  getPortfolioPins() {
    return this.http.get<PortfolioPin[]>(`${this.base}/api/portfolio`);
  }
  getPortfolioSummary() {
    return this.http.get<PortfolioSummary>(`${this.base}/api/portfolio/summary`);
  }

  // ── Admin Jobs ────────────────────────────────────────────────────────────
  completeJob(quoteRequestId: number, body: { installedDate: string; warrantyMonths: number; lat?: number | null; lng?: number | null; areaName?: string | null }) {
    return this.http.post<JobDetail>(`${this.base}/api/admin/quote-requests/${quoteRequestId}/complete`, body);
  }
  getAdminJobs(page = 1, pageSize = 20) {
    return this.http.get<{ total: number; page: number; pageSize: number; items: JobSummary[] }>(
      `${this.base}/api/admin/jobs`, { params: { page, pageSize } });
  }
  getAdminJobDetail(id: number) {
    return this.http.get<JobDetail>(`${this.base}/api/admin/jobs/${id}`);
  }
  updateAdminJob(id: number, body: { warrantyMonths: number; installedDate: string; areaName?: string | null; lat?: number | null; lng?: number | null; showInPortfolio: boolean; photoConsent: boolean }) {
    return this.http.put<{ id: number }>(`${this.base}/api/admin/jobs/${id}`, body);
  }
  uploadJobPhoto(jobId: number, file: File, type: string, caption?: string) {
    const form = new FormData();
    form.append('file', file);
    form.append('type', type);
    if (caption) form.append('caption', caption);
    return this.http.post<JobPhoto>(`${this.base}/api/admin/jobs/${jobId}/photos`, form);
  }
  deleteJobPhoto(jobId: number, photoId: number) {
    return this.http.delete(`${this.base}/api/admin/jobs/${jobId}/photos/${photoId}`);
  }
  getJobQrUrl(jobId: number) {
    return `${this.base}/api/admin/jobs/${jobId}/qr`;
  }
  getJobWarrantyPdfUrl(jobId: number) {
    return `${this.base}/api/admin/jobs/${jobId}/warranty-pdf`;
  }
  getAdminServiceRequests(status?: string, page = 1, pageSize = 20) {
    let p = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) p = p.set('status', status);
    return this.http.get<{ total: number; page: number; pageSize: number; items: AdminServiceRequest[] }>(
      `${this.base}/api/admin/service-requests`, { params: p });
  }
  updateServiceRequestStatus(id: number, status: ServiceRequestStatus) {
    return this.http.put<{ id: number; status: string }>(
      `${this.base}/api/admin/service-requests/${id}/status`, { status });
  }
}
