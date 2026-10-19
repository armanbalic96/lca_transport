import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { AiRecommendation } from '../../core/models';
import { MiComponent } from '../../shared/mi.component';
import { WizardStore } from '../wizard.store';

const SPARK = 'M12 3l1.6 4.8L18 9.4l-4.4 1.6L12 16l-1.6-4.9L6 9.4l4.4-1.6L12 3z';

function errorText(e: unknown, fallback: string): string {
  return e instanceof HttpErrorResponse && e.error?.title ? e.error.title : fallback;
}

/** AI recommendations + "Explain this result". The server recalculates the scenario and builds the prompt. */
@Component({
  selector: 'app-ai-panels',
  imports: [MiComponent],
  template: `
    <div class="grid-2" style="margin-top:0">
      <div class="ai-card">
        <div class="ai-card-head">
          <div class="ai-card-title">
            <span class="ai-chip-ic sm"><svg width="14" height="14" viewBox="0 0 24 24" aria-hidden="true"><path [attr.d]="spark" fill="#fff" /></svg></span>
            AI recommendations
          </div>
          <button class="btn sm" (click)="recommend()" [disabled]="recoBusy()">
            @if (recoBusy()) { <span class="ai-spin-wrap"><span class="ai-spin"></span><span class="ai-spin-lbl">Thinking…</span></span> }
            @else { {{ reco() ? 'Regenerate' : 'Generate' }} }
          </button>
        </div>
        @if (!reco() && !recoBusy() && !recoError()) {
          <div class="ai-empty">Get AI-generated, quantified ways to cut this scenario's transport emissions.</div>
        }
        @if (recoError()) {
          <div class="ai-status err"><app-mi name="error" [size]="16" /><span>{{ recoError() }}</span></div>
        }
        @if (reco(); as r) {
          <div class="ai-reco">
            <div class="ai-reco-headline">{{ r.headline }}</div>
            <ul class="ai-reco-list">
              @for (s of r.suggestions; track $index) {
                <li><span class="ai-bullet"><app-mi name="eco" [size]="14" /></span><span>{{ s }}</span></li>
              }
            </ul>
          </div>
        }
      </div>

      <div class="ai-card">
        <div class="ai-card-head">
          <div class="ai-card-title"><span class="ai-chip-ic sm"><app-mi name="help" [size]="14" color="#fff" /></span>Explain this result</div>
        </div>
        <div class="ai-presets">
          @for (p of presets; track p) {
            <button class="ai-preset" [class.active]="active() === p" (click)="ask(p, true)" [disabled]="explainBusy()">{{ p }}</button>
          }
        </div>
        <div class="ai-input-row tight">
          <input class="ai-input" [value]="question()" placeholder="Ask your own question about this result…" maxlength="500"
                 (input)="question.set($any($event.target).value)" (keydown.enter)="ask(question(), false)" [disabled]="explainBusy()" />
          <button class="btn sm" (click)="ask(question(), false)" [disabled]="explainBusy() || !question().trim()">
            @if (explainBusy()) { <span class="ai-spin-wrap"><span class="ai-spin"></span></span> } @else { Ask }
          </button>
        </div>
        @if (explainBusy()) {
          <div class="ai-status load"><span class="ai-spin-wrap"><span class="ai-spin"></span></span><span>Analysing the calculation…</span></div>
        }
        @if (answer() && !explainBusy()) {
          <div class="ai-answer"><app-mi name="auto_awesome" [size]="16" />{{ answer() }}</div>
        }
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiPanelsComponent {
  private readonly api = inject(ApiService);
  private readonly store = inject(WizardStore);

  readonly spark = SPARK;
  readonly presets = [
    "What drives most of this route's emissions?",
    'How much can I save by switching truck to rail?',
    'What assumptions are used in this calculation?',
  ];

  readonly reco = signal<AiRecommendation | null>(null);
  readonly recoBusy = signal(false);
  readonly recoError = signal<string | null>(null);

  readonly question = signal('');
  readonly active = signal<string | null>(null);
  readonly answer = signal<string | null>(null);
  readonly explainBusy = signal(false);

  recommend(): void {
    this.recoBusy.set(true);
    this.recoError.set(null);
    this.api.aiRecommend(this.store.state()).subscribe({
      next: r => { this.reco.set(r); this.recoBusy.set(false); },
      error: e => { this.recoError.set(errorText(e, 'Could not generate recommendations.')); this.recoBusy.set(false); },
    });
  }

  ask(q: string, preset: boolean): void {
    const query = q.trim();
    if (!query || this.explainBusy()) return;
    if (preset) this.question.set(query);
    this.active.set(preset ? query : null);
    this.answer.set(null);
    this.explainBusy.set(true);
    this.api.aiExplain(query, this.store.state()).subscribe({
      next: r => { this.answer.set(r.answer); this.explainBusy.set(false); },
      error: e => { this.answer.set(errorText(e, 'Could not answer that question.')); this.explainBusy.set(false); },
    });
  }
}
