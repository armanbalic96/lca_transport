import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FmtPipe, LEG_COLORS, city } from '../../core/format';
import { DescriptiveStats, RouteImpact, RouteStatistics } from '../../core/models';
import { MiComponent } from '../../shared/mi.component';

interface StatBar { k: string; v: number; h: number; isStd: boolean; }

/** Statistical analysis of the chosen route (Results → "Statistical analysis" → Route statistics). */
@Component({
  selector: 'app-route-stats',
  imports: [MiComponent, FmtPipe, NgTemplateOutlet],
  templateUrl: './route-stats.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RouteStatsComponent {
  readonly impact = input.required<RouteImpact>();
  readonly stats = input.required<RouteStatistics>();

  readonly colors = LEG_COLORS;
  readonly city = city;
  readonly t = computed(() => this.stats().total);
  readonly top = computed(() => this.impact().perLeg.find(l => l.key === this.t().topLegKey) ?? null);
  readonly spread = computed(() => [
    { title: 'Leg GWP', unit: 'kg CO₂e', dec: 1, bars: this.bars(this.stats().gwpSpread) },
    { title: 'Leg distance', unit: 'km', dec: 0, bars: this.bars(this.stats().distanceSpread) },
    { title: 'Leg intensity', unit: 'kg/t·km', dec: 4, bars: this.bars(this.stats().intensitySpread) },
  ]);

  private bars(d: DescriptiveStats): StatBar[] {
    const items: [string, number][] = [['min', d.min], ['mean', d.mean], ['median', d.median], ['max', d.max], ['std dev', d.std]];
    const scale = Math.max(d.min, d.mean, d.median, d.max, d.std, 1e-9);
    return items.map(([k, v]) => ({ k, v, h: Math.max(2, (v / scale) * 100), isStd: k === 'std dev' }));
  }
}
