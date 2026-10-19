import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { ReferenceDataService } from '../../core/reference-data.service';
import { ModeIconComponent } from '../../shared/mode-icon.component';

@Component({
  selector: 'app-mode-selector',
  imports: [ModeIconComponent],
  template: `
    <div class="modes">
      @for (m of modes(); track m.mode) {
        <button type="button" class="mode" [class.selected]="selected() === m.mode" (click)="selectedChange.emit(m.mode)">
          <app-mode-icon [mode]="m.mode" [size]="24" />
          <div class="name">{{ m.label }}</div>
          <div class="ef">{{ m.ef.toFixed(3) }} kgCO₂e/t·km</div>
        </button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ModeSelectorComponent {
  private readonly ref = inject(ReferenceDataService);

  readonly selected = input.required<string>();
  readonly source = input.required<string>();
  readonly selectedChange = output<string>();

  readonly modes = computed(() => {
    const f = this.ref.factors(this.source());
    return this.ref.data.transportModes.filter(m => f[m]).map(m => f[m]);
  });
}
