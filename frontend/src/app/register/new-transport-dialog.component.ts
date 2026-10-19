import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, computed, input, output, signal, viewChild } from '@angular/core';
import { MiComponent } from '../shared/mi.component';

@Component({
  selector: 'app-new-transport-dialog',
  imports: [MiComponent],
  template: `
    <div class="dlg-scrim" (mousedown)="$event.target === $event.currentTarget && cancel.emit()">
      <form class="dlg" (submit)="submit($event)" role="dialog" aria-modal="true" aria-labelledby="nt-title">
        <div class="dlg-head">
          <div id="nt-title" class="card-h2">New transport</div>
          <button type="button" class="icon-btn" (click)="cancel.emit()" aria-label="Close"><app-mi name="close" [size]="20" color="#475467" /></button>
        </div>
        <div class="dlg-body">
          <div class="card-desc" style="font-size:13px;margin-bottom:16px">Name the transport so it can be found in the register later. Cargo and route are added in the next steps.</div>
          <label class="field" style="margin-bottom:0">
            <span class="label">Name</span>
            <input #nameInput class="text-input" [value]="name()" (input)="name.set($any($event.target).value)"
                   placeholder="e.g. Precast elements, Mo i Rana – Oslo" maxlength="120" />
            @if (duplicate() || serverError()) {
              <span class="field-help" style="color:var(--color-danger)">{{ serverError() ?? 'A transport with this name already exists.' }}</span>
            }
          </label>
        </div>
        <div class="dlg-foot">
          <button type="button" class="btn" (click)="cancel.emit()">Cancel</button>
          <button type="submit" class="btn primary" [disabled]="!trimmed() || duplicate() || busy()">Add</button>
        </div>
      </form>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewTransportDialogComponent implements AfterViewInit {
  readonly existingNames = input<string[]>([]);
  readonly busy = input(false);
  readonly serverError = input<string | null>(null);
  readonly cancel = output<void>();
  readonly add = output<string>();

  readonly name = signal('');
  readonly trimmed = computed(() => this.name().trim());
  readonly duplicate = computed(() => this.existingNames().some(n => n.toLowerCase() === this.trimmed().toLowerCase()));

  private readonly nameInput = viewChild.required<ElementRef<HTMLInputElement>>('nameInput');

  ngAfterViewInit(): void {
    this.nameInput().nativeElement.focus();
  }

  submit(e: Event): void {
    e.preventDefault();
    if (this.trimmed() && !this.duplicate()) this.add.emit(this.trimmed());
  }
}
