import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { IndicatorResult } from '../../core/models';
import { MiComponent } from '../../shared/mi.component';

/** EN 15804+A2 core environmental impact indicators. Only GWP-total has factor data today. */
@Component({
  selector: 'app-indicator-table',
  imports: [MiComponent],
  template: `
    <div class="card pad-lg">
      <div class="card-head">
        <div>
          <div class="card-h3">EN 15804+A2 core environmental impact indicators</div>
          <div class="card-desc">Transport of the declared cargo over the full route · EF source: {{ sourceLabel() }}</div>
        </div>
      </div>
      <div class="ind-table" role="table">
        <div class="ind-row ind-head" role="row">
          <span role="columnheader">Indicator</span>
          <span role="columnheader">Unit</span>
          <span role="columnheader" class="num">Total</span>
        </div>
        @for (r of indicators(); track r.key) {
          <div class="ind-row" [class.empty]="r.value === null" role="row">
            <span role="cell">
              <span class="ind-abbr">{{ r.abbr }}</span>
              <span class="ind-name">{{ r.name }}</span>
            </span>
            <span role="cell" class="ind-unit">{{ r.unit }}</span>
            <span role="cell" class="num">
              @if (r.value === null) {
                <span class="ind-na">No factor</span>
              } @else if (sci(r.value); as s) {
                {{ s.mantissa }}@if (s.exp) {<span class="ind-exp">E{{ s.exp }}</span>}
              }
            </span>
          </div>
        }
      </div>
      @if (missing() > 0) {
        <div class="ind-note">
          <app-mi name="info" [size]="16" />
          <span>{{ missing() }} of {{ indicators().length }} indicators have no characterisation factors in the selected source, so no value is calculated. Add per-tonne-km factors for each transport mode to fill them in.</span>
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IndicatorTableComponent {
  readonly indicators = input.required<IndicatorResult[]>();
  readonly sourceLabel = input('');
  readonly missing = computed(() => this.indicators().filter(r => r.value === null).length);

  sci(n: number | null): { mantissa: string; exp: string | null } {
    if (!n) return { mantissa: '0', exp: null };
    const abs = Math.abs(n);
    if (abs >= 0.01 && abs < 100000) {
      return { mantissa: n.toLocaleString('en-US', { maximumFractionDigits: abs >= 100 ? 1 : 3 }), exp: null };
    }
    const [m, e] = n.toExponential(2).split('e');
    return { mantissa: m, exp: e.replace('+', '') };
  }
}
