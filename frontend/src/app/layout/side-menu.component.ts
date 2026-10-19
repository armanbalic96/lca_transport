import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { MiComponent } from '../shared/mi.component';

interface MenuItem { id: string; icon?: string; label: string; children?: MenuItem[]; }

const MENU: MenuItem[] = [
  { id: 'dashboard', icon: 'dashboard', label: 'Dashboard' },
  { id: 'epd', icon: 'description', label: 'EPD' },
  { id: 'project', icon: 'adjust', label: 'Project' },
  { id: 'analysis', icon: 'bar_chart', label: 'Analysis' },
  {
    id: 'data', icon: 'list', label: 'Data management', children: [
      { id: 'products', label: 'Products' },
      { id: 'materials', label: 'Materials' },
      { id: 'transport', label: 'Transport' },
      { id: 'production', label: 'Production' },
      { id: 'sites', label: 'Sites' },
      { id: 'connect', label: 'Connect' },
    ],
  },
  { id: 'collect', icon: 'inventory_2', label: 'Collect' },
  {
    id: 'settings', icon: 'settings', label: 'Settings', children: [
      { id: 'data-setup', label: 'Data setup' },
      { id: 'epd-register', label: 'EPD register' },
      { id: 'default-values', label: 'Default values' },
    ],
  },
];

/** LCA Tools side menu (visual shell; only "Transport" is part of this app). */
@Component({
  selector: 'app-side-menu',
  imports: [MiComponent],
  template: `
    <nav class="sidemenu">
      @for (item of menu; track item.id) {
        <div>
          <div class="menu-item" [class.active]="route() === item.id"
               (click)="item.children ? toggle(item.id) : route.set(item.id)">
            <app-mi [name]="item.icon!" [size]="22" />
            <span class="lbl">{{ item.label }}</span>
            @if (item.children) {
              <app-mi [name]="expanded()[item.id] ? 'expand_less' : 'expand_more'" [size]="20" color="#667085" />
            }
          </div>
          @if (item.children && expanded()[item.id]) {
            @for (c of item.children; track c.id) {
              <div class="sub-item" [class.active]="route() === c.id" (click)="route.set(c.id)">{{ c.label }}</div>
            }
          }
        </div>
      }
    </nav>
  `,
  styles: `:host { display: contents; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SideMenuComponent {
  readonly menu = MENU;
  readonly expanded = signal<Record<string, boolean>>({ data: true });
  readonly route = signal('transport');

  toggle(id: string): void {
    this.expanded.update(e => ({ ...e, [id]: !e[id] }));
  }
}
