import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ClientConfig, FuelOption, ModeFactor, ReferenceData } from './models';

/** Reference data and client config, loaded once at start-up (see app.config.ts). */
@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  private readonly api = inject(ApiService);

  private readonly _data = signal<ReferenceData | null>(null);
  private readonly _config = signal<ClientConfig>({ mapsBrowserApiKey: null, roadRoutingAvailable: false, aiAvailable: false });

  readonly config = this._config.asReadonly();

  get data(): ReferenceData {
    const d = this._data();
    if (!d) throw new Error('Reference data not loaded');
    return d;
  }

  async load(): Promise<void> {
    const [data, config] = await Promise.all([firstValueFrom(this.api.referenceData()), firstValueFrom(this.api.config())]);
    this._data.set(data);
    this._config.set(config);
  }

  factors(source: string): Record<string, ModeFactor> {
    const s = this.data.emissionFactorSources.find(x => x.id === source) ?? this.data.emissionFactorSources[0];
    return s.factors;
  }

  sourceLabel(source: string): { label: string; sub: string } {
    return this.data.emissionFactorSources.find(x => x.id === source) ?? this.data.emissionFactorSources[0];
  }

  modeLabel(mode: string, source = this.data.defaultSource): string {
    return this.factors(source)[mode]?.label ?? mode;
  }

  defaultLoad(mode: string): number {
    return this.data.defaultLoad[mode] ?? 60;
  }

  routingFactor(mode: string): number {
    return this.data.routingFactors[mode] ?? 1.2;
  }

  isRoadMode(mode: string | null | undefined): boolean {
    return !!mode && (mode.startsWith('truck') || mode === 'van');
  }

  fuelOptionsFor(mode: string): FuelOption[] {
    const kind = this.data.modeFuelKind[mode];
    return this.data.fuelOptions.filter(f => f.kind === 'any' || f.kind === kind);
  }
}
