import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-slider',
  template: `
    <div class="slider-wrap">
      <div class="slider-head">
        <span class="label">{{ label() }}</span>
        <span class="slider-val">{{ value().toFixed(0) }}{{ unit() ? ' ' + unit() : '' }}</span>
      </div>
      <input class="slider" type="range" [min]="min()" [max]="max()" [step]="step()" [value]="value()"
             (input)="valueChange.emit(+$any($event.target).value)" />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SliderComponent {
  readonly label = input('');
  readonly value = input.required<number>();
  readonly min = input(0);
  readonly max = input(100);
  readonly step = input(1);
  readonly unit = input('');
  readonly valueChange = output<number>();
}
