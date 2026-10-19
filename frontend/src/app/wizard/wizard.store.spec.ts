import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { ApiService } from '../core/api.service';
import { GeoLocation, RoadRouteAlternative, TransportLeg } from '../core/models';
import { ReferenceDataService } from '../core/reference-data.service';
import { WizardStore, segmentKey } from './wizard.store';

const OSLO: GeoLocation = { label: 'Oslo, Norway', country: 'Norway', lat: 59.913, lon: 10.752 };
const BERGEN: GeoLocation = { label: 'Bergen, Norway', country: 'Norway', lat: 60.39299, lon: 5.32415 };
const TRONDHEIM: GeoLocation = { label: 'Trondheim, Norway', country: 'Norway', lat: 63.4305, lon: 10.3951 };

function alt(index: number, km: number, extra: Partial<RoadRouteAlternative> = {}): RoadRouteAlternative {
  return { index, path: [], km, seconds: km * 60, fuelL: km / 3, labels: [], ferries: [], tollCount: index, ...extra };
}

describe('segmentKey', () => {
  it('returns null when either end is missing', () => {
    expect(segmentKey(null, OSLO)).toBeNull();
    expect(segmentKey(OSLO, null)).toBeNull();
  });

  it('rounds coordinates to four decimals', () => {
    expect(segmentKey(OSLO, BERGEN)).toBe('59.9130,10.7520;60.3930,5.3242');
  });

  it('is direction sensitive', () => {
    expect(segmentKey(OSLO, BERGEN)).not.toBe(segmentKey(BERGEN, OSLO));
  });
});

describe('WizardStore', () => {
  let api: {
    calculate: ReturnType<typeof vi.fn>;
    saveState: ReturnType<typeof vi.fn>;
    getTransport: ReturnType<typeof vi.fn>;
    roadRoutes: ReturnType<typeof vi.fn>;
  };
  let config: ReturnType<typeof signal<{ mapsBrowserApiKey: string | null; roadRoutingAvailable: boolean; aiAvailable: boolean }>>;
  let routes: Record<string, RoadRouteAlternative[]>;
  let store: WizardStore;

  const defaultLoads: Record<string, number> = { 'truck-large': 61, van: 40, ship: 70 };

  beforeEach(() => {
    vi.useFakeTimers();
    routes = {};
    api = {
      calculate: vi.fn(() => of({ impact: { perLeg: [] } })),
      saveState: vi.fn(() => of({})),
      getTransport: vi.fn(() => of({ id: 't1', name: 'Test transport', state: null })),
      roadRoutes: vi.fn((from: GeoLocation, to: GeoLocation) => of(routes[segmentKey(from, to)!] ?? [])),
    };
    config = signal({ mapsBrowserApiKey: null, roadRoutingAvailable: true, aiAvailable: false });
    const ref = {
      config,
      isRoadMode: (m: string | null | undefined) => !!m && (m.startsWith('truck') || m === 'van'),
      defaultLoad: (m: string) => defaultLoads[m] ?? 60,
    };

    TestBed.configureTestingModule({
      providers: [
        WizardStore,
        { provide: ApiService, useValue: api },
        { provide: ReferenceDataService, useValue: ref },
      ],
    });
    store = TestBed.inject(WizardStore);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function leg(from: GeoLocation | null, to: GeoLocation | null, mode = 'truck-large'): TransportLeg {
    return store.makeLeg({ mode }, from, to);
  }

  describe('mutators', () => {
    it('setMode switches mode and resets the load factor to the mode default', () => {
      store.patch({ loadFactor: 99 });
      store.setMode('van');
      expect(store.state().mode).toBe('van');
      expect(store.state().loadFactor).toBe(40);

      store.setMode('rail');
      expect(store.state().loadFactor).toBe(60);
    });

    it('swap exchanges from and to', () => {
      store.patch({ from: OSLO, to: BERGEN });
      store.swap();
      expect(store.state().from).toEqual(BERGEN);
      expect(store.state().to).toEqual(OSLO);
    });

    it('goTo updates step and only raises maxReached', () => {
      store.goTo(2);
      expect(store.step()).toBe(2);
      expect(store.maxReached()).toBe(2);

      store.goTo(1);
      expect(store.step()).toBe(1);
      expect(store.maxReached()).toBe(2);
    });

    it('next/back stay within the step range', () => {
      store.back();
      expect(store.step()).toBe(0);
      store.next();
      store.next();
      expect(store.isLast()).toBe(true);
      store.next();
      expect(store.step()).toBe(2);
    });

    it('pickRoute stores the choice per segment key', () => {
      store.pickRoute('a', 1);
      store.pickRoute('b', 2);
      expect(store.state().routeChoice).toEqual({ a: 1, b: 2 });
    });

    it('makeLeg defaults the load to the mode default', () => {
      expect(store.makeLeg({ mode: 'ship' }).load).toBe(70);
      expect(store.makeLeg({ mode: 'ship', load: 12 }).load).toBe(12);
    });
  });

  describe('segments', () => {
    it('single route yields one segment keyed by from/to', () => {
      store.patch({ from: OSLO, to: BERGEN, mode: 'truck-large' });
      const segs = store.segments();
      expect(segs).toHaveLength(1);
      expect(segs[0].key).toBe(segmentKey(OSLO, BERGEN));
      expect(segs[0].label).toBe('Route');
    });

    it('multi-leg yields one segment per leg', () => {
      store.patch({ multi: true, legs: [leg(OSLO, BERGEN), leg(BERGEN, null)] });
      const segs = store.segments();
      expect(segs.map(s => s.label)).toEqual(['L1', 'L2']);
      expect(segs[0].key).toBe(segmentKey(OSLO, BERGEN));
      expect(segs[1].key).toBeNull();
    });
  });

  describe('road distance write-back', () => {
    it('single route: sets roadKm from the chosen alternative', () => {
      const key = segmentKey(OSLO, BERGEN)!;
      routes[key] = [alt(0, 463), alt(1, 520)];
      store.patch({ from: OSLO, to: BERGEN, mode: 'truck-large' });
      TestBed.tick();

      expect(api.roadRoutes).toHaveBeenCalledTimes(1);
      expect(store.state().roadKm).toBe(463);

      store.pickRoute(key, 1);
      TestBed.tick();
      expect(store.state().roadKm).toBe(520);
    });

    it('single route: clears roadKm for a non-road mode', () => {
      routes[segmentKey(OSLO, BERGEN)!] = [alt(0, 463)];
      store.patch({ from: OSLO, to: BERGEN, mode: 'truck-large' });
      TestBed.tick();
      expect(store.state().roadKm).toBe(463);

      store.setMode('ship');
      TestBed.tick();
      expect(store.state().roadKm).toBeNull();
    });

    it('does not fetch routes when road routing is unavailable', () => {
      config.set({ mapsBrowserApiKey: null, roadRoutingAvailable: false, aiAvailable: false });
      store.patch({ from: OSLO, to: BERGEN, mode: 'truck-large' });
      TestBed.tick();
      expect(api.roadRoutes).not.toHaveBeenCalled();
      expect(store.state().roadKm).toBeNull();
    });

    it('multi-leg: sets legs[i].roadKm and routeInfo from the chosen alternative', () => {
      const k1 = segmentKey(OSLO, BERGEN)!;
      const k2 = segmentKey(BERGEN, TRONDHEIM)!;
      routes[k1] = [alt(0, 463, { ferries: [], tollCount: 3, fuelL: 150 })];
      routes[k2] = [alt(0, 700), alt(1, 650, { seconds: 1234, tollCount: 5, fuelL: 200 })];
      store.patch({ multi: true, legs: [leg(OSLO, BERGEN), leg(BERGEN, TRONDHEIM), leg(TRONDHEIM, OSLO, 'ship')] });
      TestBed.tick();

      store.pickRoute(k2, 1);
      TestBed.tick();

      const legs = store.state().legs;
      expect(legs[0].roadKm).toBe(463);
      expect(legs[0].routeInfo).toEqual({ seconds: 463 * 60, ferries: [], tollCount: 3, fuelL: 150 });
      expect(legs[1].roadKm).toBe(650);
      expect(legs[1].routeInfo).toEqual({ seconds: 1234, ferries: [], tollCount: 5, fuelL: 200 });
      expect(legs[2].roadKm).toBeNull();
      expect(legs[2].routeInfo).toBeNull();
      // Single-route roadKm is untouched in multi mode.
      expect(store.state().roadKm).toBeNull();
    });

    it('looks each segment up only once', () => {
      routes[segmentKey(OSLO, BERGEN)!] = [alt(0, 463)];
      store.patch({ from: OSLO, to: BERGEN, mode: 'truck-large' });
      TestBed.tick();
      store.patch({ loadFactor: 80 });
      TestBed.tick();
      expect(api.roadRoutes).toHaveBeenCalledTimes(1);
    });
  });

  describe('calculation and autosave', () => {
    it('do not start before the transport is loaded', () => {
      store.patch({ loadFactor: 50 });
      TestBed.tick();
      vi.advanceTimersByTime(1000);
      expect(api.calculate).not.toHaveBeenCalled();
      expect(api.saveState).not.toHaveBeenCalled();
    });

    it('start after load, debounced', () => {
      store.load('t1');
      TestBed.tick();
      expect(store.loaded()).toBe(true);
      expect(store.id()).toBe('t1');

      vi.advanceTimersByTime(100);
      expect(api.calculate).not.toHaveBeenCalled();
      vi.advanceTimersByTime(100);
      expect(api.calculate).toHaveBeenCalledTimes(1);
      expect(store.result()).not.toBeNull();

      vi.advanceTimersByTime(400);
      expect(api.saveState).toHaveBeenCalledTimes(1);
      expect(api.saveState.mock.calls[0][0]).toBe('t1');
      expect(store.saving()).toBe('saved');
    });

    it('navigation alone does not trigger a recalculation', () => {
      store.load('t1');
      TestBed.tick();
      vi.advanceTimersByTime(1000);
      expect(api.calculate).toHaveBeenCalledTimes(1);

      store.goTo(1);
      TestBed.tick();
      vi.advanceTimersByTime(1000);
      expect(api.calculate).toHaveBeenCalledTimes(1);
    });

    it('load merges the stored state over the defaults', () => {
      api.getTransport.mockReturnValue(of({ id: 't2', name: 'Saved', state: { mode: 'ship', loadFactor: 70 } }));
      store.load('t2');
      expect(store.name()).toBe('Saved');
      expect(store.state().mode).toBe('ship');
      expect(store.state().cargoItems.length).toBe(2);
    });
  });
});
