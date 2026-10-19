import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { GeoLocation, LegResult, TransportLeg } from '../../core/models';
import { ReferenceDataService } from '../../core/reference-data.service';
import { DistanceInputComponent, DistancePatch } from '../../shared/distance-input.component';
import { LocationPickerComponent } from '../../shared/location-picker.component';
import { MiComponent } from '../../shared/mi.component';
import { SliderComponent } from '../../shared/slider.component';
import { ToggleComponent } from '../../shared/toggle.component';

@Component({
  selector: 'app-leg-row',
  imports: [MiComponent, LocationPickerComponent, DistanceInputComponent, SliderComponent, ToggleComponent],
  templateUrl: './leg-row.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LegRowComponent {
  readonly ref = inject(ReferenceDataService);

  readonly leg = input.required<TransportLeg>();
  readonly index = input.required<number>();
  readonly source = input.required<string>();
  readonly massTonnes = input(0);
  readonly color = input<string | null>(null);
  readonly calc = input<LegResult | null>(null);
  readonly legChange = output<TransportLeg>();
  readonly remove = output<void>();

  readonly expanded = signal(false);

  readonly factors = computed(() => this.ref.factors(this.source()));
  readonly modes = computed(() => this.ref.data.transportModes.filter(m => this.factors()[m]));
  readonly vehicles = computed(() => this.ref.data.vehicleOptions[this.leg().mode] ?? []);
  readonly fuels = computed(() => this.ref.fuelOptionsFor(this.leg().mode));
  readonly isRoad = computed(() => this.ref.isRoadMode(this.leg().mode));
  readonly estimateKm = computed(() => (this.calc()?.greatCircleKm ?? 0) * this.ref.routingFactor(this.leg().mode));
  readonly hasManual = computed(() => (this.leg().manualDist ?? 0) > 0);

  set(patch: Partial<TransportLeg>): void {
    this.legChange.emit({ ...this.leg(), ...patch });
  }

  setFrom(v: GeoLocation | null): void { this.set({ from: v }); }
  setTo(v: GeoLocation | null): void { this.set({ to: v }); }
  setDistance(p: DistancePatch): void { this.set(p); }

  onModeChange(mode: string): void {
    const leg = this.leg();
    const vehicle = (this.ref.data.vehicleOptions[mode] ?? []).some(v => v.id === leg.vehicle) ? leg.vehicle : 'default';
    const fuel = this.ref.fuelOptionsFor(mode).some(f => f.id === leg.fuel) ? leg.fuel : 'default';
    this.set({ mode, vehicle, fuel, load: this.ref.defaultLoad(mode) });
  }

  duration(seconds: number): string {
    return `${Math.floor(seconds / 3600)} h ${Math.round((seconds % 3600) / 60)} min`;
  }
}
