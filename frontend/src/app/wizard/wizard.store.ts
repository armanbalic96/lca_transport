import { DestroyRef, Injectable, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap } from 'rxjs';
import { ApiService } from '../core/api.service';
import { LEG_COLORS, uid } from '../core/format';
import {
  CargoItem, GeoLocation, LegResult, LegSeed, RoadRouteAlternative, RouteInfo, ScenarioResult, TransportLeg, TransportState,
} from '../core/models';
import { ReferenceDataService } from '../core/reference-data.service';

export const STEPS = [
  { kicker: 'Step 1', title: 'Cargo' },
  { kicker: 'Step 2', title: 'Route' },
  { kicker: 'Step 3', title: 'Results' },
];

/** A drawable route segment: the direct route, or one leg of a multi-leg route. */
export interface MapSegment {
  key: string | null;
  from: GeoLocation | null;
  to: GeoLocation | null;
  mode: string;
  color: string;
  label: string;
  legIndex: number;
  alts: RoadRouteAlternative[];
  choiceIdx: number;
}

export function segmentKey(from: GeoLocation | null, to: GeoLocation | null): string | null {
  if (!from || !to) return null;
  return `${from.lat.toFixed(4)},${from.lon.toFixed(4)};${to.lat.toFixed(4)},${to.lon.toFixed(4)}`;
}

export function newCargoItem(seed: Partial<CargoItem> = {}): CargoItem {
  return {
    id: uid(),
    byVolume: !!seed.byVolume,
    mass: seed.mass ?? 1,
    massUnit: seed.massUnit ?? 't',
    material: seed.material ?? 'concrete',
    volume: seed.volume ?? 1,
  };
}

/**
 * State for one open transport. The backend is the single source of truth for every number:
 * the store posts the state to /api/calculations (debounced) and autosaves it to the register.
 */
@Injectable()
export class WizardStore {
  private readonly api = inject(ApiService);
  private readonly ref = inject(ReferenceDataService);
  private readonly destroyRef = inject(DestroyRef);

  readonly id = signal<string | null>(null);
  readonly name = signal('');
  readonly loaded = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saving = signal<'idle' | 'saving' | 'saved' | 'error'>('idle');

  readonly state = signal<TransportState>(this.defaultState());
  readonly result = signal<ScenarioResult | null>(null);
  readonly calcError = signal(false);

  /** Google road alternatives per segment key; [] means "looked up, none". */
  private readonly roadAlts = signal<Record<string, RoadRouteAlternative[]>>({});
  private readonly pendingRoutes = new Set<string>();

  readonly step = computed(() => this.state().step);
  readonly maxReached = computed(() => this.state().maxReached);
  readonly isLast = computed(() => this.step() === STEPS.length - 1);

  readonly segments = computed<MapSegment[]>(() => {
    const s = this.state();
    const alts = this.roadAlts();
    const raw = s.multi
      ? s.legs.map((l, i) => ({ from: l.from, to: l.to, mode: l.mode, color: LEG_COLORS[i % LEG_COLORS.length], label: `L${i + 1}`, legIndex: i }))
      : [{ from: s.from, to: s.to, mode: s.mode, color: '#127575', label: 'Route', legIndex: 0 }];
    return raw.map(seg => {
      const key = segmentKey(seg.from, seg.to);
      const list = key && this.ref.isRoadMode(seg.mode) ? alts[key] ?? [] : [];
      const choiceIdx = Math.min((key && s.routeChoice[key]) || 0, Math.max(0, list.length - 1));
      return { ...seg, key, alts: list, choiceIdx };
    });
  });

  constructor() {
    // Only start calculating/saving once the transport has been loaded.
    const loadedState = toObservable(computed(() => (this.loaded() ? this.state() : null)));

    // Recalculate when anything except navigation changes.
    loadedState.pipe(
      filter((s): s is TransportState => s !== null),
      map(s => JSON.stringify({ ...s, step: 0, maxReached: 0, routeChoice: {} })),
      distinctUntilChanged(),
      debounceTime(150),
      switchMap(json => this.api.calculate(JSON.parse(json) as TransportState).pipe(
        catchError(() => { this.calcError.set(true); return of(null); }),
      )),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(r => {
      if (r) { this.result.set(r); this.calcError.set(false); }
    });

    // Autosave into the transport register (the server recomputes the register summary).
    loadedState.pipe(
      filter((s): s is TransportState => s !== null && !!this.id()),
      debounceTime(500),
      switchMap(s => {
        this.saving.set('saving');
        return this.api.saveState(this.id()!, s).pipe(
          map(() => 'saved' as const),
          catchError(() => of('error' as const)),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(status => this.saving.set(status));

    // Fetch Google road alternatives for road segments we haven't looked up yet.
    effect(() => {
      if (!this.ref.config().roadRoutingAvailable) return;
      const known = this.roadAlts();
      for (const seg of this.segments()) {
        if (!seg.key || !seg.from || !seg.to || !this.ref.isRoadMode(seg.mode)) continue;
        if (known[seg.key] !== undefined || this.pendingRoutes.has(seg.key)) continue;
        const key = seg.key;
        this.pendingRoutes.add(key);
        this.api.roadRoutes(seg.from, seg.to).pipe(catchError(() => of([]))).subscribe(alts => {
          this.pendingRoutes.delete(key);
          this.roadAlts.update(c => ({ ...c, [key]: alts }));
        });
      }
    });

    // Write the chosen road distance back into the state so every calculation uses the same number.
    effect(() => {
      const segs = this.segments();
      const s = this.state();
      const picks = segs.map(seg => {
        const alt = seg.alts[seg.choiceIdx];
        const info: RouteInfo | null = alt ? { seconds: alt.seconds, ferries: alt.ferries, tollCount: alt.tollCount, fuelL: alt.fuelL } : null;
        return { km: alt ? alt.km : null, info };
      });

      if (!s.multi) {
        const km = picks[0]?.km ?? null;
        if (s.roadKm !== km) this.state.update(x => ({ ...x, roadKm: km }));
        return;
      }
      let changed = false;
      const legs = s.legs.map((l, i) => {
        const p = picks[i];
        if ((l.roadKm ?? null) === p.km && JSON.stringify(l.routeInfo ?? null) === JSON.stringify(p.info)) return l;
        changed = true;
        return { ...l, roadKm: p.km, routeInfo: p.info };
      });
      if (changed) this.state.update(x => ({ ...x, legs }));
    });
  }

  load(id: string): void {
    this.api.getTransport(id).subscribe({
      next: t => {
        this.id.set(t.id);
        this.name.set(t.name);
        if (t.state) this.state.set({ ...this.defaultState(), ...t.state });
        this.loaded.set(true);
      },
      error: () => this.loadError.set('This transport could not be loaded.'),
    });
  }

  // ---- Mutators ----
  patch(p: Partial<TransportState>): void {
    this.state.update(s => ({ ...s, ...p }));
  }

  setMode(mode: string): void {
    this.patch({ mode, loadFactor: this.ref.defaultLoad(mode) });
  }

  swap(): void {
    this.state.update(s => ({ ...s, from: s.to, to: s.from }));
  }

  setCargo(items: CargoItem[]): void {
    this.patch({ cargoItems: items });
  }

  setLegs(legs: TransportLeg[]): void {
    this.patch({ legs });
  }

  setMulti(multi: boolean): void {
    this.patch({ multi });
  }

  pickRoute(key: string, idx: number): void {
    this.state.update(s => ({ ...s, routeChoice: { ...s.routeChoice, [key]: idx } }));
  }

  goTo(step: number): void {
    this.state.update(s => ({ ...s, step, maxReached: Math.max(s.maxReached, step) }));
    document.querySelector('.shell-main')?.scrollTo(0, 0);
  }

  next(): void { if (this.step() < STEPS.length - 1) this.goTo(this.step() + 1); }
  back(): void { if (this.step() > 0) this.goTo(this.step() - 1); }

  makeLeg(seed: Partial<LegSeed> = {}, from: GeoLocation | null = null, to: GeoLocation | null = null): TransportLeg {
    const mode = seed.mode ?? 'truck-large';
    return {
      id: uid(), from, to, mode,
      vehicle: seed.vehicle ?? 'default',
      fuel: seed.fuel ?? 'default',
      load: seed.load ?? this.ref.defaultLoad(mode),
      ret: !!seed.ret,
      manualDist: null,
      distUnit: 'km',
      roadKm: null,
      routeInfo: null,
    };
  }

  legResult(index: number): LegResult | null {
    return this.result()?.impact.perLeg[index] ?? null;
  }

  private defaultState(): TransportState {
    return {
      step: 0,
      maxReached: 0,
      source: 'en15804',
      cargoItems: [
        newCargoItem({ mass: 18, massUnit: 't', material: 'concrete' }),
        newCargoItem({ mass: 6, massUnit: 't', material: 'steel' }),
      ],
      from: { label: 'Mo i Rana, Norway', country: 'Norway', lat: 66.314, lon: 14.142 },
      to: { label: 'Oslo, Norway', country: 'Norway', lat: 59.913, lon: 10.752 },
      mode: 'truck-large',
      loadFactor: 61,
      manualDist: null,
      distUnit: 'km',
      roadKm: null,
      multi: false,
      legs: [],
      routeChoice: {},
    };
  }
}
