import { ChangeDetectionStrategy, Component, inject, input, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ShellComponent } from '../layout/shell.component';
import { MiComponent } from '../shared/mi.component';
import { CargoEditorComponent } from './cargo/cargo-editor.component';
import { ResultsStepComponent } from './results/results-step.component';
import { RouteStepComponent } from './route/route-step.component';
import { StepperComponent } from './stepper.component';
import { STEPS, WizardStore } from './wizard.store';

/** Transport GWP calculator — step-by-step wizard: Cargo → Route → Results. One route, one total. */
@Component({
  selector: 'app-wizard',
  imports: [ShellComponent, MiComponent, RouterLink, StepperComponent, CargoEditorComponent, RouteStepComponent, ResultsStepComponent],
  providers: [WizardStore],
  templateUrl: './wizard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WizardComponent implements OnInit {
  readonly store = inject(WizardStore);
  private readonly router = inject(Router);

  /** Route parameter (component input binding). */
  readonly id = input.required<string>();
  readonly stepCount = STEPS.length;

  ngOnInit(): void {
    this.store.load(this.id());
  }

  exit(): void {
    this.router.navigate(['/']);
  }
}
