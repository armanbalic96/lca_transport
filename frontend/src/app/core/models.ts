// API contracts — mirror the C# models in TransportGwp.Domain / TransportGwp.Application.

export interface GeoLocation {
  label: string;
  full?: string | null;
  country?: string | null;
  lat: number;
  lon: number;
}

export interface FerryCrossing { text: string; km: number; minutes: number; }

export interface RouteInfo {
  seconds: number;
  ferries: FerryCrossing[];
  tollCount: number;
  fuelL?: number | null;
}

export interface TransportLeg {
  id: string;
  from: GeoLocation | null;
  to: GeoLocation | null;
  mode: string;
  vehicle: string;
  fuel: string;
  load: number;
  ret: boolean;
  manualDist: number | null;
  distUnit: DistUnit;
  roadKm?: number | null;
  routeInfo?: RouteInfo | null;
}

export type DistUnit = 'km' | 'mi';

export interface CargoItem {
  id: string;
  byVolume: boolean;
  /** Tonnes (internal unit). */
  mass: number;
  massUnit: string;
  material: string;
  /** m³ */
  volume: number;
}

export interface TransportState {
  step: number;
  maxReached: number;
  source: string;
  cargoItems: CargoItem[];
  from: GeoLocation | null;
  to: GeoLocation | null;
  mode: string;
  loadFactor: number;
  manualDist: number | null;
  distUnit: DistUnit;
  roadKm: number | null;
  multi: boolean;
  legs: TransportLeg[];
  routeChoice: Record<string, number>;
}

// ---- Reference data ----
export interface ModeFactor { mode: string; ef: number; label: string; sub: string; }
export interface EmissionFactorSource { id: string; label: string; sub: string; factors: Record<string, ModeFactor>; }
export interface VehicleOption { id: string; label: string; mult: number; }
export interface FuelOption { id: string; label: string; mult: number; kind: string; }
export interface Material { id: string; name: string; density: number; }
export interface MassUnit { id: string; short: string; perTonne: number; }
export interface LegSeed { mode: string; vehicle: string; fuel: string; load: number; ret: boolean; }
export interface LegTemplate { id: string; label: string; sub: string; legs: LegSeed[]; }

export interface ReferenceData {
  defaultSource: string;
  transportModes: string[];
  emissionFactorSources: EmissionFactorSource[];
  defaultLoad: Record<string, number>;
  vehicleOptions: Record<string, VehicleOption[]>;
  fuelOptions: FuelOption[];
  modeFuelKind: Record<string, string>;
  materials: Material[];
  massUnits: MassUnit[];
  legTemplates: LegTemplate[];
  routingFactors: Record<string, number>;
  roadModes: string[];
}

export interface ClientConfig {
  mapsBrowserApiKey: string | null;
  roadRoutingAvailable: boolean;
  aiAvailable: boolean;
}

// ---- Calculation results ----
export interface LegResult {
  index: number;
  key: string;
  mode: string;
  modeLabel: string;
  from: string | null;
  to: string | null;
  ret: boolean;
  load: number;
  greatCircleKm: number;
  distanceKm: number;
  distanceSource: 'road' | 'manual' | 'estimate';
  baseEf: number;
  vehicleMult: number;
  fuelMult: number;
  loadAdj: number;
  effectiveEf: number;
  trips: number;
  tkm: number;
  gwp: number;
  share: number;
}

export interface RouteImpact {
  source: string;
  sourceLabel: string;
  massTonnes: number;
  legCount: number;
  distanceKm: number;
  tkm: number;
  gwp: number;
  intensity: number;
  gwpPerKg: number;
  perLeg: LegResult[];
}

export interface ModeAlternative {
  mode: string; modeLabel: string; gwp: number; intensity: number; distanceKm: number; deltaPct: number; isCurrent: boolean;
}
export interface ModeRecommendation { mode: string; modeLabel: string; gwp: number; reductionPct: number; text: string; }

export interface ModeComparison {
  current: { mode: string | null; modeLabel: string; gwp: number };
  best: ModeAlternative | null;
  worst: ModeAlternative | null;
  savingsVsWorst: number;
  savingsVsCurrent: number;
  isCurrentBest: boolean;
  ranked: ModeAlternative[];
  recommendations: ModeRecommendation[];
}

export interface ValidationMessage { level: 'ok' | 'warning' | 'error'; text: string; }
export interface RouteValidation { valid: boolean; level: 'ok' | 'warning' | 'error'; distanceKm: number; messages: ValidationMessage[]; }

export interface MaterialShare { name: string; tonnes: number; pct: number; }
export interface CargoSummary { totalTonnes: number; byMaterial: MaterialShare[]; label: string; count: number; }

export interface DescriptiveStats { n: number; min: number; max: number; mean: number; median: number; std: number; sum: number; cv: number; }

export interface RouteStatistics {
  total: {
    gwp: number; distanceKm: number; tkm: number; intensity: number; gwpPerKm: number; gwpPerKg: number;
    legCount: number; modeCount: number; meanPerLeg: number; topLegKey: string | null;
  };
  modeShares: { label: string; gwp: number; share: number }[];
  gwpSpread: DescriptiveStats;
  distanceSpread: DescriptiveStats;
  intensitySpread: DescriptiveStats;
}

export interface IndicatorResult { key: string; abbr: string; name: string; unit: string; value: number | null; }

export interface ScenarioResult {
  cargo: CargoSummary;
  validation: RouteValidation;
  greatCircleKm: number;
  impact: RouteImpact;
  comparison: ModeComparison;
  statistics: RouteStatistics;
  indicators: IndicatorResult[];
}

// ---- Register ----
export interface TransportSummary {
  from: string | null; to: string | null; mode: string | null; modeLabel: string | null;
  massTonnes: number | null; gwp: number | null; distanceKm: number | null; maxReached: number;
}
export interface TransportListItem { id: string; name: string; createdAt: string; updatedAt: string; summary: TransportSummary | null; }
export interface TransportDetail { id: string; name: string; createdAt: string; updatedAt: string; state: TransportState | null; }

// ---- Geo ----
export interface PlaceSuggestion { label: string; country: string; full: string | null; placeId: string | null; location: GeoLocation | null; }

export interface RoadRouteAlternative {
  index: number;
  path: [number, number][];
  km: number;
  seconds: number;
  fuelL: number | null;
  labels: string[];
  ferries: FerryCrossing[];
  tollCount: number;
}

// ---- AI ----
export interface AiRecommendation { headline: string; suggestions: string[]; }

export type ExportFormat = 'report' | 'csv' | 'json';
