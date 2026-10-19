import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { LEG_COLORS } from '../../core/format';
import { TransportLeg } from '../../core/models';
import { ReferenceDataService } from '../../core/reference-data.service';
import { MiComponent } from '../../shared/mi.component';
import { ToggleComponent } from '../../shared/toggle.component';
import { WizardStore } from '../wizard.store';
import { LegRowComponent } from './leg-row.component';

/** Multi-leg breakdown: intermediate stops, mode changes, templates. */
@Component({
  selector: 'app-legs-editor',
  imports: [MiComponent, ToggleComponent, LegRowComponent],
  templateUrl: './legs-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LegsEditorComponent {
  readonly store = inject(WizardStore);
  readonly ref = inject(ReferenceDataService);
  readonly colors = LEG_COLORS;

  readonly state = this.store.state;
  readonly templates = computed(() => this.ref.data.legTemplates);
  readonly impact = computed(() => this.store.result()?.impact ?? null);
  readonly massTonnes = computed(() => this.impact()?.massTonnes ?? 0);

  enable(): void {
    const s = this.state();
    if (s.legs.length === 0) {
      this.store.patch({ multi: true, legs: [this.store.makeLeg({ mode: s.mode, load: s.loadFactor }, s.from, s.to)] });
    } else {
      this.store.setMulti(true);
    }
  }

  toggle(): void {
    if (this.state().multi) this.store.setMulti(false);
    else this.enable();
  }

  addLeg(): void {
    const legs = this.state().legs;
    const last = legs[legs.length - 1];
    this.store.setLegs([
      ...legs,
      this.store.makeLeg({ mode: last?.mode ?? this.state().mode, vehicle: last?.vehicle, fuel: last?.fuel, load: last?.load }, last?.to ?? null, null),
    ]);
  }

  applyTemplate(id: string): void {
    const tpl = this.templates().find(t => t.id === id);
    if (!tpl) return;
    const s = this.state();
    const legs = tpl.legs.map((seed, i) =>
      this.store.makeLeg(seed, i === 0 ? s.from : null, i === tpl.legs.length - 1 ? s.to : null));
    this.store.patch({ legs, multi: true });
  }

  onTemplateSelect(select: HTMLSelectElement): void {
    if (select.value) this.applyTemplate(select.value);
    select.value = '';
  }

  updateLeg(id: string, next: TransportLeg): void {
    this.store.setLegs(this.state().legs.map(l => (l.id === id ? next : l)));
  }

  removeLeg(id: string): void {
    this.store.setLegs(this.state().legs.filter(l => l.id !== id));
  }
}
