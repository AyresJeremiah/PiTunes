import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SongService } from '../../services/song.service';
import { YouTubeSearchResult } from '../../models/song.model';
import { ToastService } from 'app/services/toast.service';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.scss']
})
export class SearchComponent {
  query: string = '';
  results: YouTubeSearchResult[] = [];
  nowPlaying: YouTubeSearchResult | null = null;
  poolingStarted: boolean = false;
  isQueueing: { [id: string]: boolean } = {};
  isSearching: boolean = false;


  constructor(private songService: SongService, private toastService: ToastService) { }

  search(): void {
    this.isSearching = true;
    this.songService.search(this.query).subscribe({
      next: res => {
        this.results = res;
        this.isSearching = false;
      },
      error: () => {
        this.isSearching = false;
      }
    });
  }
  queueSong(song: YouTubeSearchResult): void {
    this.isQueueing[song.id] = true;
    this.songService.queue(song).subscribe({
      next: () => {
        this.toastService.show('Queued successfully!');
      },
      error: () => {
        this.toastService.show('Failed to queue song.');
      },
      complete: () => {
        this.isQueueing[song.id] = false;
      }
    });
  }

  refreshNowPlaying(): void {
    this.songService.getNowPlaying().subscribe(res => {
      this.nowPlaying = res;
    });
    this.setPooling();
  }

  skip(): void {
    this.songService.skip().subscribe(() => {
      // console.log("Song skipped");
    });
  }

  setPooling(): void {
    if(!this.poolingStarted) {
      this.poolingStarted = true;
      setInterval(() => this.refreshNowPlaying(), 5000);
    }
  }


}
