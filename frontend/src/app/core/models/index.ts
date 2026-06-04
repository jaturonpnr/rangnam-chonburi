import type { MultiLineString } from 'geojson';

export type Material = 'Galvanized' | 'Stainless';
export type QuoteStatus = 'New' | 'Contacted' | 'Quoted' | 'Won' | 'Lost';
export type JobSource = 'Quote' | 'FacebookImport' | 'Manual';

export interface GutterProduct {
  id: number;
  material: Material;
  sizeInches: number;
  pricePerMeter: number;
  isActive: boolean;
}

export interface BuildingType {
  id: number;
  label: string;
  sizeInches: number;
  displayOrder: number;
  isActive: boolean;
}

export interface ServiceZone {
  id: number;
  name: string;
  travelSurcharge: number;
  isActive: boolean;
}

export interface ShopProfilePublic {
  shopName: string;
  phone: string;
  lineOaLink: string;
}

export interface BreakdownItem {
  label: string;
  amount: number;
}

export interface EstimateResult {
  breakdown: BreakdownItem[];
  total: number;
  disclaimer: string;
}

export interface QuoteRequestSummary {
  id: number;
  quoteNumber: string;
  customerName: string;
  phone: string;
  estimatedTotal: number;
  status: QuoteStatus;
  createdAt: string;
}

export interface QuoteRequestDetail extends QuoteRequestSummary {
  address: string | null;
  locationDetail: string | null;
  serviceZoneName: string | null;
  buildingTypeLabel: string | null;
  material: Material;
  sizeInches: number;
  lengthMeters: number;
  downspoutCount: number;
  floors: number;
  removeOld: boolean;
  breakdownJson: string;
  measureSource: MeasureSource;
  measuredLengthMeters: number | null;
  measuredGeoJson: string | null;
  mapCenterLat: number | null;
  mapCenterLng: number | null;
  mapZoom: number | null;
}

export interface PricingConfig {
  id: number;
  minimumMeters: number;
  downspoutPricePerPoint: number;
  heightSurchargePercent: number;
  removalPricePerMeter: number;
  surveyFee: number;
}

export interface ShopProfile {
  id: number;
  shopName: string;
  phone: string;
  address: string;
  logoUrl: string | null;
  lineOaLink: string;
  quoteValidityDays: number;
  quoteFooterNote: string;
}

export type MeasureSource = 'Manual' | 'Map';

export interface MapMeasureResult {
  geojson: MultiLineString;
  measuredLengthMeters: number;
  centerLat: number;
  centerLng: number;
  zoom: number;
}

export interface StatsResponse {
  totalLeads: number;
  leadsThisMonth: number;
  leadsThisWeek: number;
  totalEstimatedValue: number;
  averageQuoteValue: number;
  byStatus: { status: string; count: number }[];
  byZone: { zone: string; count: number }[];
  weeklySeries: { week: string; count: number }[];
  topProducts: { label: string; count: number }[];
}

// ── CR3: Warranty + Portfolio ─────────────────────────────────────────────

export type PhotoType = 'Before' | 'After' | 'Other';
export type ServiceRequestType = 'WarrantyClaim' | 'Maintenance' | 'Other';
export type ServiceRequestStatus = 'New' | 'Contacted' | 'Done';

export interface JobPhoto {
  id: number;
  url: string;
  type: PhotoType;
  caption: string | null;
  displayOrder: number;
}

export interface ServiceRequestItem {
  id: number;
  contactPhone: string;
  customerNote: string | null;
  type: ServiceRequestType;
  status: ServiceRequestStatus;
  createdAt: string;
}

export interface JobSummary {
  id: number;
  warrantyNumber: string | null;
  quoteNumber: string | null;
  installedDate: string | null;
  warrantyExpiry: string | null;
  material: Material;
  sizeInches: number;
  lengthMeters: number;
  showInPortfolio: boolean;
  photoConsent: boolean;
  serviceRequestCount: number;
  source: JobSource;
}

export interface JobDetail {
  id: number;
  quoteRequestId: number | null;
  quoteNumber: string | null;
  warrantyNumber: string | null;
  publicToken: string | null;
  installedDate: string | null;
  warrantyMonths: number | null;
  warrantyExpiry: string | null;
  material: Material;
  sizeInches: number;
  lengthMeters: number;
  downspoutCount: number;
  lat: number | null;
  lng: number | null;
  approxLat: number | null;
  approxLng: number | null;
  areaName: string | null;
  showInPortfolio: boolean;
  photoConsent: boolean;
  source: JobSource;
  importBatchId: number | null;
  photos: JobPhoto[];
  serviceRequests: ServiceRequestItem[];
}

export interface WarrantyCard {
  warrantyNumber: string;
  installedDate: string;
  warrantyExpiry: string;
  material: Material;
  sizeInches: number;
  lengthMeters: number;
  downspoutCount: number;
  photos: JobPhoto[];
  shopName: string;
  shopPhone: string;
  lineOaLink: string;
}

export interface PortfolioPin {
  jobId: number;
  approxLat: number;
  approxLng: number;
  areaName: string | null;
  material: Material;
  installedDate: string;
  consentedPhotos: JobPhoto[];
}

export interface PortfolioSummary {
  total: number;
  byArea: { name: string; count: number }[];
}

export interface AdminServiceRequest {
  id: number;
  contactPhone: string;
  customerNote: string | null;
  type: ServiceRequestType;
  status: ServiceRequestStatus;
  createdAt: string;
  jobId: number;
  warrantyNumber: string;
}

// ── CR4: Portfolio Import ─────────────────────────────────────────────────────

export interface ImportDraftItem {
  jobId: number;
  areaName: string | null;
  material: Material;
  sizeInches: number;
  lengthMeters: number;
  approxLat: number | null;
  approxLng: number | null;
  showInPortfolio: boolean;
  photoConsent: boolean;
  photos: JobPhoto[];
  importBatchId: number;
  createdAt: string;
}

export interface ImportBatchSummary {
  id: number;
  source: string;
  photoCount: number;
  jobCount: number;
  createdAt: string;
}
