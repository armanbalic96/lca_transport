import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { FmtPipe, city, downloadBlob } from '../../core/format';
import { ExportFormat, ModeAlternative } from '../../core/models';
import { ReferenceDataService } from '../../core/reference-data.service';
import { MiComponent } from '../../shared/mi.component';
import { ModeIconComponent } from '../../shared/mode-icon.component';
import { WizardStore } from '../wizard.store';
import { AiPanelsComponent } from './ai-panels.component';
import { IndicatorTableComponent } from './indicator-table.component';
import { RouteStatsComponent } from './route-stats.component';

/** Step 3 — totals, per-leg breakdown, alternative modes, exports, statistics and AI panels. */
@Component({
  selector: 'app-results-step',
  imports: [MiComponent, ModeIconComponent, FmtPipe, RouteStatsComponent, IndicatorTableComponent, AiPanelsComponent],
  templateUrl: './results-step.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultsStepComponent {
  private readonly api = inject(ApiService);
  readonly store = inject(WizardStore);
  readonly ref = inject(ReferenceDataService);

  readonly tab = signal<'results' | 'stats'>('results');
  readonly statsView = signal<'route' | 'indicators'>('route');
  readonly copied = signal(false);

  readonly state = this.store.state;
  readonly result = this.store.result;
  readonly city = city;

  readonly impact = computed(() => this.result()?.impact ?? null);
  readonly source = computed(() => this.ref.sourceLabel(this.state().source));
  readonly modeLabel = computed(() => this.ref.modeLabel(this.state().mode, this.state().source));
  readonly direct = computed(() => (this.state().multi ? null : this.impact()?.perLeg[0] ?? null));
  readonly ranked = computed(() => this.result()?.comparison.ranked ?? []);
  readonly cmpMax = computed(() => {
    const r = this.ranked();
    return r.length ? r[r.length - 1].gwp : 1;
  });

  readonly totalSub = computed(() => {
    const s = this.state();
    return s.multi ? `Multi-leg · ${s.legs.length} leg${s.legs.length !== 1 ? 's' : ''}` : this.modeLabel();
  });

  readonly distanceSub = computed(() => {
    if (this.state().multi) return 'Sum of all legs';
    const src = this.direct()?.distanceSource;
    const how = src === 'manual' ? 'Entered distance' : src === 'road' ? 'Google road distance' : 'Routing factor applied';
    return `${how} · ${this.modeLabel()}`;
  });

  isCurrent(alt: ModeAlternative): boolean {
    return alt.isCurrent && !this.state().multi;
  }

  exportAs(format: ExportFormat): void {
    const ext = format === 'report' ? 'txt' : format;
    this.api.export(format, this.state(), this.store.name()).subscribe(blob => downloadBlob(blob, `transport-scenario.${ext}`));
  }

  copyReport(): void {
    this.api.export('report', this.state(), this.store.name()).subscribe(async blob => {
      try {
        await navigator.clipboard.writeText(await blob.text());
        this.copied.set(true);
        setTimeout(() => this.copied.set(false), 1800);
      } catch {
        // Clipboard not available (e.g. insecure context) — ignore.
      }
    });
  }
}
