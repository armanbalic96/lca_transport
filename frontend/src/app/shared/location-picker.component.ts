import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, HostListener, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap, tap } from 'rxjs';
import { ApiService } from '../core/api.service';
import { GeoLocation, PlaceSuggestion } from '../core/models';
import { MiComponent } from './mi.component';

/** Place search via the backend (Google Places, Nominatim fallback). */
@Component({
  selector: 'app-location-picker',
  imports: [MiComponent],
  template: `
    <div class="loc-picker">
      <div class="lca-field">
        <app-mi name="location_on" [size]="compact() ? 18 : 20" />
        <input [value]="query()" [placeholder]="placeholder()"
               (input)="onInput($any($event.target).value)" (focus)="open.set(true)" (keydown)="onKey($event)" />
      </div>
      @if (open() && query().length >= 2) {
        <div class="loc-results">
          @if (loading()) {
            <div class="empty">Searching…</div>
          } @else if (results().length === 0) {
            <div class="empty">No matches. Try a city name.</div>
          } @else {
            @for (r of results(); track $index) {
              <div class="loc-result" [class.active]="$index === focusIdx()"
                   (mouseenter)="focusIdx.set($index)" (mousedown)="$event.preventDefault(); select(r)">
                <app-mi name="place" [size]="18" />
                <div>
                  <div class="name">{{ r.label }}</div>
                  <div class="country">{{ r.country }}</div>
                </div>
              </div>
            }
          }
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocationPickerComponent {
  private readonly api = inject(ApiService);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly value = input<GeoLocation | null>(null);
  readonly placeholder = input('Search city, port, address…');
  readonly compact = input(false);
  readonly valueChange = output<GeoLocation | null>();

  readonly query = signal('');
  readonly results = signal<PlaceSuggestion[]>([]);
  readonly open = signal(false);
  readonly loading = signal(false);
  readonly focusIdx = signal(-1);

  private readonly search$ = new Subject<string>();
  private typing = false;

  constructor() {
    // Keep the text in sync when the value changes from outside (swap, template, load) —
    // but don't wipe what the user is typing when we cleared the value ourselves.
    effect(() => {
      const v = this.value();
      if (v) this.query.set(v.label);
      else if (!this.typing) this.query.set('');
    });

    this.search$.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      tap(() => this.loading.set(true)),
      switchMap(q => this.api.autocomplete(q).pipe(catchError(() => of([])))),
      takeUntilDestroyed(inject(DestroyRef)),
    ).subscribe(r => {
      this.results.set(r);
      this.loading.set(false);
    });
  }

  @HostListener('document:mousedown', ['$event'])
  onDocumentMouseDown(e: MouseEvent): void {
    if (!this.host.nativeElement.contains(e.target as Node)) this.open.set(false);
  }

  onInput(text: string): void {
    this.typing = true;
    this.query.set(text);
    this.open.set(true);
    this.focusIdx.set(-1);
    if (this.value()) this.valueChange.emit(null);
    if (text.trim().length >= 2) this.search$.next(text.trim());
    else this.results.set([]);
  }

  onKey(e: KeyboardEvent): void {
    const results = this.results();
    if (e.key === 'ArrowDown') { e.preventDefault(); this.focusIdx.update(i => Math.min(i + 1, results.length - 1)); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); this.focusIdx.update(i => Math.max(i - 1, 0)); }
    else if (e.key === 'Enter' && results[this.focusIdx()]) { e.preventDefault(); this.select(results[this.focusIdx()]); }
    else if (e.key === 'Escape') this.open.set(false);
  }

  select(r: PlaceSuggestion): void {
    this.typing = false;
    this.query.set(r.label);
    this.open.set(false);
    this.results.set([]);
    if (r.location) { this.valueChange.emit(r.location); return; }
    if (!r.placeId) return;
    this.loading.set(true);
    this.api.placeDetails(r.placeId).subscribe({
      next: d => { this.loading.set(false); this.valueChange.emit(d); },
      error: () => this.loading.set(false),
    });
  }
}
