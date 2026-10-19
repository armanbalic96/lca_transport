import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MiComponent } from '../shared/mi.component';
import { STEPS } from './wizard.store';

@Component({
  selector: 'app-stepper',
  imports: [MiComponent],
  template: `
    <div class="stepper-bar">
      <div class="stepper">
        @for (s of steps; track $index; let i = $index) {
          <div class="step-node" [class.done]="i < step()" [class.active]="i === step()" [class.clickable]="i <= maxReached()"
               (click)="i <= maxReached() && jump.emit(i)">
            <div class="step-circle">
              @if (i < step()) { <app-mi name="check" [size]="18" color="#fff" /> } @else { {{ i + 1 }} }
            </div>
            <div class="step-meta">
              <div class="step-kicker">{{ s.kicker }}</div>
              <div class="step-title">{{ s.title }}</div>
            </div>
          </div>
          @if (i < steps.length - 1) {
            <div class="step-line" [class.done]="i < step()"></div>
          }
        }
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepperComponent {
  readonly steps = STEPS;
  readonly step = input.required<number>();
  readonly maxReached = input.required<number>();
  readonly jump = output<number>();
}
