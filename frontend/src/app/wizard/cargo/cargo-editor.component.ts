import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { FmtPipe } from '../../core/format';
import { CargoItem, CargoSummary, Material } from '../../core/models';
import { ReferenceDataService } from '../../core/reference-data.service';
import { MiComponent } from '../../shared/mi.component';
import { newCargoItem } from '../wizard.store';

/** Multi-item cargo editor (Step 1). Each item is entered by mass or by volume with a density lookup. */
@Component({
  selector: 'app-cargo-editor',
  imports: [MiComponent, FmtPipe],
  templateUrl: './cargo-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CargoEditorComponent {
  private readonly ref = inject(ReferenceDataService);

  readonly items = input.required<CargoItem[]>();
  /** Server-computed summary; null while the first calculation is in flight. */
  readonly summary = input<CargoSummary | null>(null);
  readonly itemsChange = output<CargoItem[]>();

  readonly materials = computed(() => this.ref.data.materials);
  readonly massUnits = computed(() => this.ref.data.massUnits);
  readonly materialColors = ['--cargo-c1', '--cargo-c2', '--cargo-c3', '--cargo-c4', '--cargo-c5', '--cargo-c6'];

  material(item: CargoItem): Material {
    return this.materials().find(m => m.id === item.material) ?? this.materials()[0];
  }

  /** Item mass in tonnes — display only; the total comes from the server. */
  massT(item: CargoItem): number {
    return item.byVolume ? (item.volume || 0) * this.material(item).density / 1000 : item.mass || 0;
  }

  perTonne(item: CargoItem): number {
    return this.massUnits().find(u => u.id === item.massUnit)?.perTonne ?? 1;
  }

  displayMass(item: CargoItem): number {
    const coarse = item.massUnit === 'kg' || item.massUnit === 'lb';
    return +(item.mass * this.perTonne(item)).toFixed(coarse ? 0 : 3);
  }

  update(id: string, patch: Partial<CargoItem>): void {
    this.itemsChange.emit(this.items().map(it => (it.id === id ? { ...it, ...patch } : it)));
  }

  setDisplayMass(item: CargoItem, raw: string): void {
    this.update(item.id, { mass: (parseFloat(raw) || 0) / this.perTonne(item) });
  }

  remove(id: string): void {
    this.itemsChange.emit(this.items().filter(it => it.id !== id));
  }

  add(): void {
    const last = this.items()[this.items().length - 1];
    this.itemsChange.emit([...this.items(), newCargoItem({ byVolume: last?.byVolume, massUnit: last?.massUnit })]);
  }
}
