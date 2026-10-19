import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, NgZone, OnDestroy, computed, effect, inject, input, output, signal, viewChild,
} from '@angular/core';
import { fmt } from '../../core/format';
import { RoadRouteAlternative } from '../../core/models';
import { MapsLoaderService } from '../../core/maps-loader.service';
import { MiComponent } from '../../shared/mi.component';
import { MapSegment } from '../wizard.store';

export interface RouteFact { label: string; value: string; accent?: string; }

interface Tip { x: number; y: number; seg: MapSegment; alt: RoadRouteAlternative; chosen: boolean; }

const MAP_STYLES: google.maps.MapTypeStyle[] = [
  { featureType: 'poi', stylers: [{ visibility: 'off' }] },
  { featureType: 'transit', stylers: [{ visibility: 'off' }] },
  { featureType: 'water', elementType: 'geometry', stylers: [{ color: '#dbe6e6' }] },
  { featureType: 'landscape', elementType: 'geometry', stylers: [{ color: '#f6f7f6' }] },
];

/**
 * Route map. Road segments show every Google alternative (grey) with the chosen one in the leg colour;
 * hovering shows distance, drive time and GWP, clicking picks that route. Other modes are drawn as dashed lines.
 */
@Component({
  selector: 'app-map-view',
  imports: [MiComponent],
  templateUrl: './map-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewComponent implements AfterViewInit, OnDestroy {
  private readonly loader = inject(MapsLoaderService);
  private readonly zone = inject(NgZone);

  readonly segments = input.required<MapSegment[]>();
  /** kg CO2-eq per t·km × trips for a segment's leg, from the server calculation. */
  readonly rateFor = input.required<(seg: MapSegment) => number>();
  readonly massTonnes = input(0);
  readonly pick = output<{ key: string; idx: number }>();

  readonly failed = signal(false);
  readonly tip = signal<Tip | null>(null);
  readonly hasAlts = computed(() => this.segments().some(s => s.alts.length > 1));

  private readonly el = viewChild.required<ElementRef<HTMLDivElement>>('mapEl');
  private map: google.maps.Map | null = null;
  private drawn: (google.maps.Polyline | google.maps.Marker)[] = [];
  private ready = signal(false);
  private lastBoundsSig = '';

  constructor() {
    effect(() => {
      const segs = this.segments();
      if (this.ready()) this.draw(segs);
    });
  }

  async ngAfterViewInit(): Promise<void> {
    try {
      const maps = await this.loader.load();
      this.map = new maps.Map(this.el().nativeElement, {
        center: { lat: 60, lng: 10 }, zoom: 5,
        mapTypeControl: false, streetViewControl: false, fullscreenControl: false,
        styles: MAP_STYLES,
      });
      this.ready.set(true);
    } catch {
      this.failed.set(true);
    }
  }

  ngOnDestroy(): void {
    this.clear();
  }

  facts(t: Tip): RouteFact[] {
    const rate = this.rateFor()(t.seg);
    const mass = this.massTonnes();
    const gwp = mass * t.alt.km * rate;
    const chosenAlt = t.seg.alts[t.seg.choiceIdx];
    const dGwp = chosenAlt ? gwp - mass * chosenAlt.km * rate : 0;
    const h = Math.floor(t.alt.seconds / 3600);
    const m = Math.round((t.alt.seconds % 3600) / 60);
    const rows: RouteFact[] = [
      { label: 'Distance', value: `${fmt(t.alt.km, 0)} km` },
      { label: 'Drive time', value: t.alt.seconds ? `${h} h ${m} min` : '—' },
      { label: 'Transport GWP', value: `${fmt(gwp, 1)} kg CO₂e` },
    ];
    if (t.alt.fuelL) rows.push({ label: 'Fuel (car model)', value: `${fmt(t.alt.fuelL, 1)} L` });
    if (t.alt.ferries.length) rows.push({ label: 'Ferry crossings', value: String(t.alt.ferries.length) });
    if (t.alt.tollCount) rows.push({ label: 'Toll sections', value: String(t.alt.tollCount) });
    if (Math.abs(dGwp) > 0.05) {
      rows.push({
        label: 'vs selected',
        value: `${dGwp > 0 ? '+' : ''}${fmt(dGwp, 1)} kg CO₂e`,
        accent: dGwp > 0 ? '#B4503B' : 'var(--color-primary)',
      });
    }
    return rows;
  }

  /** Keep the tooltip inside the map box — flip left near the right edge. */
  tipPos(t: Tip): { left: number; top: number } {
    const el = this.el().nativeElement;
    const w = 212, h = 175, pad = 8;
    let left = t.x + 14;
    if (left + w + pad > el.clientWidth) left = t.x - w - 14;
    left = Math.max(pad, Math.min(left, Math.max(pad, el.clientWidth - w - pad)));
    const top = Math.max(pad, Math.min(t.y - 12, Math.max(pad, el.clientHeight - h - pad)));
    return { left, top };
  }

  legendText(s: MapSegment): string {
    const ends = s.from && s.to ? ` — ${s.from.label.split(',')[0]} → ${s.to.label.split(',')[0]}` : '';
    return s.label + ends;
  }

  roadKm(s: MapSegment): number | null {
    return s.alts[s.choiceIdx]?.km ?? null;
  }

  private clear(): void {
    this.drawn.forEach(o => o.setMap(null));
    this.drawn = [];
  }

  private draw(segments: MapSegment[]): void {
    const map = this.map;
    if (!map) return;
    this.clear();
    const bounds = new google.maps.LatLngBounds();
    let any = false;

    const px = (e: google.maps.MapMouseEvent) => {
      const r = this.el().nativeElement.getBoundingClientRect();
      const de = e.domEvent as MouseEvent;
      return { x: de.clientX - r.left, y: de.clientY - r.top };
    };

    for (const seg of segments) {
      if (!seg.from || !seg.to) continue;
      any = true;
      const a = { lat: seg.from.lat, lng: seg.from.lon };
      const b = { lat: seg.to.lat, lng: seg.to.lon };

      if (seg.alts.length) {
        // Unselected alternatives first, so the chosen route draws on top.
        seg.alts.forEach((alt, i) => {
          if (alt.path.length < 2) return;
          const chosen = i === seg.choiceIdx;
          const path = alt.path.map(([lat, lng]) => ({ lat, lng }));
          const line = new google.maps.Polyline({
            path, map, clickable: true,
            strokeColor: chosen ? seg.color : '#98A2B3',
            strokeOpacity: chosen ? 0.95 : 0.6,
            strokeWeight: chosen ? 5 : 4,
            zIndex: chosen ? 20 : 5,
          });
          line.addListener('mouseover', (e: google.maps.MapMouseEvent) => this.zone.run(() => {
            if (!chosen) line.setOptions({ strokeColor: '#475467', strokeOpacity: 0.9, zIndex: 10 });
            this.tip.set({ ...px(e), seg, alt, chosen });
          }));
          line.addListener('mousemove', (e: google.maps.MapMouseEvent) => this.zone.run(() => {
            this.tip.update(t => (t ? { ...t, ...px(e) } : t));
          }));
          line.addListener('mouseout', () => this.zone.run(() => {
            if (!chosen) line.setOptions({ strokeColor: '#98A2B3', strokeOpacity: 0.6, zIndex: 5 });
            this.tip.set(null);
          }));
          line.addListener('click', () => this.zone.run(() => {
            this.tip.set(null);
            if (seg.key) this.pick.emit({ key: seg.key, idx: i });
          }));
          this.drawn.push(line);
          path.forEach(p => bounds.extend(p));
        });
      } else {
        this.drawn.push(new google.maps.Polyline({
          path: [a, b], map, strokeColor: seg.color, strokeOpacity: 0, geodesic: true,
          icons: [{ icon: { path: 'M 0,-1 0,1', strokeOpacity: 0.95, strokeColor: seg.color, strokeWeight: 3.5, scale: 3 }, offset: '0', repeat: '16px' }],
        }));
        bounds.extend(a);
        bounds.extend(b);
      }

      for (const [pos, kind] of [[a, 'FROM'], [b, 'TO']] as const) {
        this.drawn.push(new google.maps.Marker({
          position: pos, map, title: `${seg.label} · ${kind}`, zIndex: 30,
          icon: { path: google.maps.SymbolPath.CIRCLE, scale: 7, fillColor: seg.color, fillOpacity: 1, strokeColor: '#fff', strokeWeight: 2.5 },
          label: { text: seg.label, color: '#1A2731', fontSize: '11px', fontWeight: '600', className: 'gm-leg-label' },
        }));
      }
    }

    // Only re-fit when the geography changed, not when the user picks another alternative.
    const boundsSig = segments.map(s => `${s.key}:${s.alts.length}`).join('|');
    if (any && boundsSig !== this.lastBoundsSig) {
      map.fitBounds(bounds, 60);
      this.lastBoundsSig = boundsSig;
    }
  }
}
