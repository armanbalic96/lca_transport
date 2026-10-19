import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MiComponent } from '../shared/mi.component';

/** LCA Tools top bar. Company/user pills are static placeholders until wired to the platform's identity. */
@Component({
  selector: 'app-toolbar',
  imports: [MiComponent],
  template: `
    <div class="toolbar">
      <div class="logo"><img src="assets/lca-logo-white.svg" alt="LCA.no" /></div>
      <div class="tb-icon"><app-mi name="menu" [size]="22" color="#fff" /></div>
      <div class="tb-brand">
        <span>LCA Tools</span>
        <app-mi name="chevron_right" [size]="18" color="#fff" style="opacity:.7" />
        <span class="crumb">Data management</span>
      </div>
      <div class="spacer"></div>
      <div class="tb-pill"><app-mi name="business" [size]="18" color="#fff" />Company<app-mi name="expand_more" [size]="16" color="#fff" style="opacity:.7" /></div>
      <div class="tb-icon"><app-mi name="notifications" [size]="22" color="#fff" /></div>
      <div class="tb-pill"><app-mi name="help_outline" [size]="18" color="#fff" />Support<app-mi name="expand_more" [size]="16" color="#fff" style="opacity:.7" /></div>
      <div class="tb-pill"><app-mi name="language" [size]="18" color="#fff" />EN<app-mi name="expand_more" [size]="16" color="#fff" style="opacity:.7" /></div>
      <div class="tb-pill"><app-mi name="person" [size]="18" color="#fff" />User<app-mi name="expand_more" [size]="16" color="#fff" style="opacity:.7" /></div>
    </div>
  `,
  styles: `:host { display: contents; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToolbarComponent {}
