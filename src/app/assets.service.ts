import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Asset } from './asset.model';

@Injectable({ providedIn: 'root' })
export class AssetsService {
  private readonly http = inject(HttpClient);

  getAssets() {
    return this.http.get<Asset[]>('/api/assets');
  }
}
