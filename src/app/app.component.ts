import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Asset } from './asset.model';
import { AssetsService } from './assets.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  protected readonly assetsService = inject(AssetsService);
  protected assets: Asset[] = [];
  protected error = '';

  ngOnInit(): void {
    this.assetsService.getAssets().subscribe({
      next: (assets) => {
        this.assets = assets;
      },
      error: () => {
        this.error = 'Could not load assets right now. Please try again later.';
      }
    });
  }
}
