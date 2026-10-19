import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { DistUnit } from '../core/models';

const MI_TO_KM = 1.609344;

export interface DistancePatch { manualDist: number | null; distUnit: DistUnit; }

/** Distance entry for modes without a road network. The value is kept in the chosen unit. */
@Component({
  selector: 'app-distance-input',
  template: `
    <div class="input-unit" [style.height.px]="compact() ? 34 : null" [style.width.px]="compact() ? 200 : null"
         [style.maxWidth.px]="compact() ? null : 280">
      <input type="number" min="0" step="1" [value]="value() ?? ''" [placeholder]="placeholder()"
             (input)="onValue($any($event.target).value)" />
      <select [value]="unit()" (change)="switchUnit($any($event.target).value)" aria-label="Distance unit">
        <option value="km">km</option>
        <option value="mi">miles</option>
      </select>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DistanceInputComponent {
  readonly value = input<number | null>(null);
  readonly unit = input<DistUnit>('km');
  readonly estimateKm = input(0);
  readonly compact = input(false);
  readonly changed = output<DistancePatch>();

  readonly placeholder = computed(() => {
    const est = this.estimateKm();
    if (!(est > 0)) return 'Distance';
    return `Estimate: ${(this.unit() === 'mi' ? est / MI_TO_KM : est).toFixed(0)}`;
  });

  onValue(raw: string): void {
    this.changed.emit({ manualDist: raw === '' ? null : Math.max(0, parseFloat(raw) || 0), distUnit: this.unit() });
  }

  switchUnit(next: DistUnit): void {
    const v = this.value();
    const converted = v && v > 0 ? +(next === 'mi' ? v / MI_TO_KM : v * MI_TO_KM).toFixed(1) : null;
    this.changed.emit({ manualDist: converted, distUnit: next });
  }
}
