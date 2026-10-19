import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Material Icons glyph (LCA Tools uses Material Icons throughout). */
@Component({
  selector: 'app-mi',
  template: `<span class="material-icons mi-ico" [style.fontSize.px]="size()" [style.color]="color()" style="line-height:1">{{ name() }}</span>`,
  styles: `:host { display: inline-flex; align-items: center; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MiComponent {
  readonly name = input.required<string>();
  readonly size = input(22);
  readonly color = input<string | undefined>(undefined);
}
