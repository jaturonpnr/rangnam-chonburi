import type { MultiLineString } from 'geojson';

export type Material = 'Galvanized' | 'Stainless';
export type QuoteStatus = 'New' | 'Contacted' | 'Quoted' | 'Won' | 'Lost';

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
