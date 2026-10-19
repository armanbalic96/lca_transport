import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../core/api.service';
import { city } from '../core/format';
import { TransportListItem } from '../core/models';
import { ShellComponent } from '../layout/shell.component';
import { MiComponent } from '../shared/mi.component';
import { ModeIconComponent } from '../shared/mode-icon.component';
import { NewTransportDialogComponent } from './new-transport-dialog.component';

@Component({
  selector: 'app-transport-register',
  imports: [ShellComponent, MiComponent, ModeIconComponent, NewTransportDialogComponent],
  templateUrl: './transport-register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransportRegisterComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly items = signal<TransportListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly q = signal('');
  readonly confirmId = signal<string | null>(null);
  readonly dialogOpen = signal(false);
  readonly creating = signal(false);
  readonly createError = signal<string | null>(null);

  readonly names = computed(() => this.items().map(t => t.name));
  readonly list = computed(() => {
    const q = this.q().toLowerCase();
    return this.items()
      .filter(t => !q || [t.name, t.summary?.from, t.summary?.to].some(s => s && s.toLowerCase().includes(q)))
      .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
  });

  readonly city = city;

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.listTransports().subscribe({
      next: items => { this.items.set(items); this.loading.set(false); this.error.set(null); },
      error: () => { this.loading.set(false); this.error.set('Could not load transports. Is the API running?'); },
    });
  }

  open(id: string): void {
    this.router.navigate(['/transports', id]);
  }

  openDialog(): void {
    this.createError.set(null);
    this.dialogOpen.set(true);
  }

  create(name: string): void {
    this.creating.set(true);
    this.api.createTransport(name).subscribe({
      next: t => { this.creating.set(false); this.dialogOpen.set(false); this.open(t.id); },
      error: (e: HttpErrorResponse) => {
        this.creating.set(false);
        this.createError.set(e.status === 409 ? 'A transport with this name already exists.' : 'Could not create the transport.');
      },
    });
  }

  duplicate(id: string): void {
    this.api.duplicateTransport(id).subscribe(copy => this.items.update(xs => [...xs, copy]));
  }

  remove(id: string): void {
    this.api.deleteTransport(id).subscribe(() => {
      this.items.update(xs => xs.filter(t => t.id !== id));
      this.confirmId.set(null);
    });
  }

  isDone(t: TransportListItem): boolean {
    return (t.summary?.maxReached ?? 0) >= 2;
  }

  regNum(n: number | null | undefined, d = 0): string {
    return n != null && Number.isFinite(n)
      ? n.toLocaleString('nb-NO', { maximumFractionDigits: d, minimumFractionDigits: d })
      : '—';
  }

  regDate(iso: string): string {
    const d = new Date(iso);
    const p = (n: number) => String(n).padStart(2, '0');
    return `${p(d.getDate())}.${p(d.getMonth() + 1)}.${d.getFullYear()} ${p(d.getHours())}:${p(d.getMinutes())}`;
  }
}
