import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { isPlatformServer } from '@angular/common';
import { FeatureOptions } from 'src/app/models/featureOptions.model';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  private isServer(): boolean {
    return isPlatformServer(this.platformId);
  }

  getConfig(): Observable<FeatureOptions> {
    if (this.isServer()) {
      // Prevent SSR crash by returning fallback
      return of({ aiEnabled: false });
    }

    return this.http.get<FeatureOptions>('/api/config');
  }
}
