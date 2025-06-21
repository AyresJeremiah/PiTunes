import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {FeatureOptions} from 'src/app/models/featureOptions.model';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  constructor(private http: HttpClient) {}
  getConfig(): Observable<FeatureOptions> {
    return this.http.get<FeatureOptions>('/api/config');
  }
}
