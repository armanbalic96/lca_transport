import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SideMenuComponent } from './side-menu.component';
import { ToolbarComponent } from './toolbar.component';

/** App shell: toolbar, side menu and the scrolling main area (projected content). */
@Component({
  selector: 'app-shell',
  imports: [ToolbarComponent, SideMenuComponent],
  template: `
    <div class="shell">
      <app-toolbar />
      <div class="shell-body">
        <app-side-menu />
        <main class="shell-main"><ng-content /></main>
      </div>
    </div>
  `,
  styles: `:host { display: contents; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {}
