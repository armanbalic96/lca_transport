import { Pipe, PipeTransform } from '@angular/core';

/** ≥1000 → no decimals, ≥10 → one decimal, otherwise up to `d` decimals (en-US grouping). */
export function fmt(n: number | null | undefined, d = 1): string {
  if (n == null || !Number.isFinite(n)) return '—';
  const abs = Math.abs(n);
  if (abs >= 1000) return n.toLocaleString('en-US', { maximumFractionDigits: 0 });
  if (abs >= 10) return n.toLocaleString('en-US', { maximumFractionDigits: 1 });
  return n.toLocaleString('en-US', { maximumFractionDigits: d });
}

@Pipe({ name: 'fmt' })
export class FmtPipe implements PipeTransform {
  transform(n: number | null | undefined, d = 1): string {
    return fmt(n, d);
  }
}

export function city(label: string | null | undefined): string {
  return label ? label.split(',')[0] : '—';
}

export function uid(): string {
  return Math.random().toString(36).slice(2, 9);
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export const LEG_COLORS = ['#127575', '#E58370', '#5b8fc4', '#d8a13a', '#8a6fb0', '#5aa06e'];
