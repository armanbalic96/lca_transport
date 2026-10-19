import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AiRecommendation, ClientConfig, ExportFormat, GeoLocation, PlaceSuggestion, ReferenceData,
  RoadRouteAlternative, ScenarioResult, TransportDetail, TransportListItem, TransportState,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  referenceData(): Observable<ReferenceData> {
    return this.http.get<ReferenceData>('/api/referencedata');
  }

  config(): Observable<ClientConfig> {
    return this.http.get<ClientConfig>('/api/config');
  }

  calculate(state: TransportState): Observable<ScenarioResult> {
    return this.http.post<ScenarioResult>('/api/calculations', state);
  }

  export(format: ExportFormat, state: TransportState, name?: string): Observable<Blob> {
    const params: Record<string, string> = name ? { name } : {};
    return this.http.post(`/api/calculations/export/${format}`, state, { params, responseType: 'blob' });
  }

  // ---- Register ----
  listTransports(): Observable<TransportListItem[]> {
    return this.http.get<TransportListItem[]>('/api/transports');
  }

  getTransport(id: string): Observable<TransportDetail> {
    return this.http.get<TransportDetail>(`/api/transports/${encodeURIComponent(id)}`);
  }

  createTransport(name: string): Observable<TransportDetail> {
    return this.http.post<TransportDetail>('/api/transports', { name });
  }

  saveState(id: string, state: TransportState): Observable<TransportListItem> {
    return this.http.put<TransportListItem>(`/api/transports/${encodeURIComponent(id)}/state`, state);
  }

  duplicateTransport(id: string): Observable<TransportListItem> {
    return this.http.post<TransportListItem>(`/api/transports/${encodeURIComponent(id)}/duplicate`, {});
  }

  deleteTransport(id: string): Observable<void> {
    return this.http.delete<void>(`/api/transports/${encodeURIComponent(id)}`);
  }

  // ---- Geo ----
  autocomplete(q: string): Observable<PlaceSuggestion[]> {
    return this.http.get<PlaceSuggestion[]>('/api/geo/autocomplete', { params: { q } });
  }

  placeDetails(placeId: string): Observable<GeoLocation> {
    return this.http.get<GeoLocation>(`/api/geo/places/${encodeURIComponent(placeId)}`);
  }

  roadRoutes(from: GeoLocation, to: GeoLocation): Observable<RoadRouteAlternative[]> {
    return this.http.post<RoadRouteAlternative[]>('/api/geo/road-routes', { from, to });
  }

  // ---- AI ----
  aiRecommend(state: TransportState): Observable<AiRecommendation> {
    return this.http.post<AiRecommendation>('/api/ai/recommendations', { state });
  }

  aiExplain(question: string, state: TransportState): Observable<{ answer: string }> {
    return this.http.post<{ answer: string }>('/api/ai/explain', { question, state });
  }
}
