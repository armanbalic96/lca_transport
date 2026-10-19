import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DistUnit, GeoLocation } from '../../core/models';
import { ReferenceDataService } from '../../core/reference-data.service';
import { DistanceInputComponent, DistancePatch } from '../../shared/distance-input.component';
import { LocationPickerComponent } from '../../shared/location-picker.component';
import { MiComponent } from '../../shared/mi.component';
import { SliderComponent } from '../../shared/slider.component';
import { MapSegment, WizardStore } from '../wizard.store';
import { LegsEditorComponent } from './legs-editor.component';
import { MapViewComponent } from './map-view.component';
import { ModeSelectorComponent } from './mode-selector.component';

/** Step 2 — origin, destination, mode, optional legs and the route map. */
@Component({
  selector: 'app-route-step',
  imports: [MiComponent, LocationPickerComponent, ModeSelectorComponent, DistanceInputComponent, SliderComponent, LegsEditorComponent, MapViewComponent],
  templateUrl: './route-step.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RouteStepComponent {
  readonly store = inject(WizardStore);
  readonly ref = inject(ReferenceDataService);

  readonly state = this.store.state;
  readonly legsOpen = signal(this.store.state().multi);

  readonly result = this.store.result;
  readonly direct = computed(() => (this.state().multi ? null : this.result()?.impact.perLeg[0] ?? null));
  readonly isRoad = computed(() => this.ref.isRoadMode(this.state().mode));
  readonly modeLabel = computed(() => this.ref.modeLabel(this.state().mode, this.state().source));
  readonly estimateKm = computed(() => (this.result()?.greatCircleKm ?? 0) * this.ref.routingFactor(this.state().mode));
  readonly validation = computed(() => this.result()?.validation ?? null);
  readonly lastMessage = computed(() => {
    const v = this.validation();
    return v ? v.messages[v.messages.length - 1] : null;
  });

  readonly distanceLabel = computed(() => {
    const d = this.direct();
    if (d?.distanceSource === 'manual') return 'Entered distance';
    if (d?.distanceSource === 'road') return 'Google road distance';
    if (this.isRoad() && this.ref.config().roadRoutingAvailable) return 'estimating road route…';
    return 'with routing factor';
  });

  /** Effective kg CO2-eq per t·km (incl. return trips) for a map segment's leg — used for hover GWP. */
  readonly rateFor = (seg: MapSegment): number => {
    const leg = this.result()?.impact.perLeg[seg.legIndex];
    return leg ? leg.effectiveEf * leg.trips : 0;
  };

  setFrom(v: GeoLocation | null): void { this.store.patch({ from: v }); }
  setTo(v: GeoLocation | null): void { this.store.patch({ to: v }); }
  setManual(p: DistancePatch): void { this.store.patch({ manualDist: p.manualDist, distUnit: p.distUnit as DistUnit }); }
}
