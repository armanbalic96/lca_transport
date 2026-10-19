import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MiComponent } from './mi.component';

const MODE_MI: Record<string, string> = {
  'truck-small': 'local_shipping',
  'truck-large': 'local_shipping',
  'truck-electric': 'electric_bolt',
  van: 'airport_shuttle',
  rail: 'train',
  'sea-container': 'directions_boat',
  'sea-bulk': 'sailing',
  inland: 'kayaking',
  ferry: 'directions_boat_filled',
  air: 'flight',
};

/** Transport mode icon. Material Icons has no container-ship glyph, so that one is drawn inline. */
@Component({
  selector: 'app-mode-icon',
  imports: [MiComponent],
  template: `
    @if (mode() === 'sea-container') {
      <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none"
           [attr.stroke]="color() || 'currentColor'" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"
           style="display:block" aria-hidden="true">
        <path d="M2.2 16.4h19.6l-2.1 4H4.3l-2.1-4Z" />
        <path d="M4.2 16.4v-3.1h15.6v3.1" />
        <path d="M6.1 13.3v-2.2h9.4v2.2" />
        <path d="M8.4 11.1V9.2h4.8v1.9" />
        <path d="M17.1 13.3V8.6h2.4v4.7" />
        <path d="M18.3 8.6V6.4" />
      </svg>
    } @else {
      <app-mi [name]="icon()" [size]="size()" [color]="color()" />
    }
  `,
  styles: `:host { display: inline-flex; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ModeIconComponent {
  readonly mode = input.required<string>();
  readonly size = input(24);
  readonly color = input<string | undefined>(undefined);

  icon(): string {
    return MODE_MI[this.mode()] ?? 'local_shipping';
  }
}
