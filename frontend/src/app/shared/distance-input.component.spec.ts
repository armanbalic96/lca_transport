import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DistanceInputComponent, DistancePatch } from './distance-input.component';

describe('DistanceInputComponent', () => {
  let fixture: ComponentFixture<DistanceInputComponent>;
  let cmp: DistanceInputComponent;
  let emitted: DistancePatch[];

  beforeEach(() => {
    fixture = TestBed.createComponent(DistanceInputComponent);
    cmp = fixture.componentInstance;
    emitted = [];
    cmp.changed.subscribe(p => emitted.push(p));
  });

  function setInputs(inputs: Record<string, unknown>): void {
    for (const [k, v] of Object.entries(inputs)) fixture.componentRef.setInput(k, v);
  }

  it('converts km to miles when switching unit', () => {
    setInputs({ value: 100, unit: 'km' });
    cmp.switchUnit('mi');
    expect(emitted).toEqual([{ manualDist: 62.1, distUnit: 'mi' }]);
  });

  it('converts miles to km when switching unit', () => {
    setInputs({ value: 100, unit: 'mi' });
    cmp.switchUnit('km');
    expect(emitted).toEqual([{ manualDist: 160.9, distUnit: 'km' }]);
  });

  it('emits null when switching unit with no value', () => {
    setInputs({ value: null });
    cmp.switchUnit('mi');
    expect(emitted).toEqual([{ manualDist: null, distUnit: 'mi' }]);
  });

  it('emits the typed value in the current unit, clamped at zero', () => {
    setInputs({ unit: 'mi' });
    cmp.onValue('42.5');
    cmp.onValue('-3');
    cmp.onValue('');
    expect(emitted).toEqual([
      { manualDist: 42.5, distUnit: 'mi' },
      { manualDist: 0, distUnit: 'mi' },
      { manualDist: null, distUnit: 'mi' },
    ]);
  });

  it('shows the estimate in the chosen unit as placeholder', () => {
    expect(cmp.placeholder()).toBe('Distance');
    setInputs({ estimateKm: 160.9344, unit: 'km' });
    expect(cmp.placeholder()).toBe('Estimate: 161');
    setInputs({ unit: 'mi' });
    expect(cmp.placeholder()).toBe('Estimate: 100');
  });

  it('renders the input from the DOM and emits on input', async () => {
    setInputs({ value: 5, unit: 'km' });
    await fixture.whenStable();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    expect(input.value).toBe('5');
    input.value = '7';
    input.dispatchEvent(new Event('input'));
    expect(emitted).toEqual([{ manualDist: 7, distUnit: 'km' }]);
  });
});
