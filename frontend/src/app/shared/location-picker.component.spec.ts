import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../core/api.service';
import { GeoLocation, PlaceSuggestion } from '../core/models';
import { LocationPickerComponent } from './location-picker.component';

const OSLO: GeoLocation = { label: 'Oslo, Norway', country: 'Norway', lat: 59.913, lon: 10.752 };
const BERGEN: GeoLocation = { label: 'Bergen, Norway', country: 'Norway', lat: 60.393, lon: 5.324 };

/** Mimics a parent that writes valueChange back into [value]. */
@Component({
  imports: [LocationPickerComponent],
  template: `<app-location-picker [value]="value()" (valueChange)="onChange($event)" />`,
})
class HostComponent {
  readonly value = signal<GeoLocation | null>(OSLO);
  readonly emitted: (GeoLocation | null)[] = [];
  onChange(v: GeoLocation | null): void {
    this.emitted.push(v);
    this.value.set(v);
  }
}

describe('LocationPickerComponent', () => {
  let api: { autocomplete: ReturnType<typeof vi.fn>; placeDetails: ReturnType<typeof vi.fn> };
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let picker: LocationPickerComponent;
  let input: HTMLInputElement;

  beforeEach(async () => {
    api = {
      autocomplete: vi.fn(() => of([])),
      placeDetails: vi.fn(() => of(BERGEN)),
    };
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: ApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    await fixture.whenStable();
    picker = fixture.debugElement.children[0].componentInstance;
    input = fixture.nativeElement.querySelector('input');
  });

  function type(text: string): void {
    input.value = text;
    input.dispatchEvent(new Event('input'));
  }

  it('shows the label of the bound value', () => {
    expect(picker.query()).toBe('Oslo, Norway');
    expect(input.value).toBe('Oslo, Norway');
  });

  it('typing clears the value without wiping the typed text', async () => {
    type('Ber');
    await fixture.whenStable();

    expect(host.emitted).toEqual([null]);
    expect(host.value()).toBeNull();
    expect(picker.query()).toBe('Ber');
    expect(input.value).toBe('Ber');
  });

  it('an external reset to null clears the text when not typing', async () => {
    host.value.set(null);
    await fixture.whenStable();
    expect(picker.query()).toBe('');
  });

  it('searches the backend after the debounce', async () => {
    vi.useFakeTimers();
    try {
      type('Be');
      type('Ber');
      vi.advanceTimersByTime(350);
      expect(api.autocomplete).toHaveBeenCalledTimes(1);
      expect(api.autocomplete).toHaveBeenCalledWith('Ber');
    } finally {
      vi.useRealTimers();
    }
  });

  it('selecting a Nominatim hit emits its location', async () => {
    const hit: PlaceSuggestion = { label: 'Bergen, Norway', country: 'Norway', full: null, placeId: null, location: BERGEN };
    type('Ber');
    picker.select(hit);
    await fixture.whenStable();

    expect(api.placeDetails).not.toHaveBeenCalled();
    expect(host.emitted).toEqual([null, BERGEN]);
    expect(picker.query()).toBe('Bergen, Norway');
    expect(picker.open()).toBe(false);
  });

  it('selecting a Google hit resolves it via placeDetails', async () => {
    const hit: PlaceSuggestion = { label: 'Bergen', country: 'Norway', full: 'Bergen, Norway', placeId: 'g-123', location: null };
    type('Ber');
    picker.select(hit);
    await fixture.whenStable();

    expect(api.placeDetails).toHaveBeenCalledWith('g-123');
    expect(host.emitted).toEqual([null, BERGEN]);
    expect(picker.loading()).toBe(false);
    expect(picker.query()).toBe('Bergen, Norway');
  });

  it('Enter selects the focused result', () => {
    const hit: PlaceSuggestion = { label: 'Bergen, Norway', country: 'Norway', full: null, placeId: null, location: BERGEN };
    picker.results.set([hit]);
    picker.onKey(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    picker.onKey(new KeyboardEvent('keydown', { key: 'Enter' }));
    expect(host.emitted).toEqual([BERGEN]);
  });
});
