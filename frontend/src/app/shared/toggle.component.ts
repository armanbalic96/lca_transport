import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-toggle',
  template: `
    <button type="button" class="toggle" [class.big]="big()" [class.on]="on()" [attr.aria-pressed]="on()" (click)="toggled.emit()">
      <span class="knob"></span>
    </button>
  `,
  styles: `:host { display: inline-flex; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToggleComponent {
  readonly on = input(false);
  readonly big = input(false);
  readonly toggled = output<void>();
}
