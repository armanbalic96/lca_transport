import { Injectable, inject } from '@angular/core';
import { ReferenceDataService } from './reference-data.service';

/** Loads the Google Maps JavaScript API once, with the referrer-restricted browser key from /api/config. */
@Injectable({ providedIn: 'root' })
export class MapsLoaderService {
  private readonly ref = inject(ReferenceDataService);
  private loading: Promise<typeof google.maps> | null = null;

  load(): Promise<typeof google.maps> {
    if (this.loading) return this.loading;
    const key = this.ref.config().mapsBrowserApiKey;
    if (!key) return Promise.reject(new Error('No Google Maps browser key configured'));

    this.loading = new Promise((resolve, reject) => {
      if (window.google?.maps) { resolve(window.google.maps); return; }
      const cb = '__gmapsReady';
      (window as unknown as Record<string, () => void>)[cb] = () => resolve(window.google.maps);
      const timer = setTimeout(() => reject(new Error('Google Maps did not load')), 12000);
      const s = document.createElement('script');
      s.async = true;
      s.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(key)}&loading=async&callback=${cb}`;
      s.onerror = () => { clearTimeout(timer); reject(new Error('Google Maps failed to load')); };
      document.head.appendChild(s);
    });
    this.loading.catch(() => (this.loading = null));
    return this.loading;
  }
}
