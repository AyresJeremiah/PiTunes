import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SongService } from 'app/services/song.service';
import { ToastService } from 'app/services/toast.service';
import { YouTubeItem } from 'app/models/song.model';
import {SongStateService} from 'src/app/services/song.state.service';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.scss']
})
export class SearchComponent implements OnInit, OnDestroy {
  query: string = '';
  results: YouTubeItem[] = [];
  nowPlaying: YouTubeItem | null = null;
  isQueueing: { [id: string]: boolean } = {};
  isQueued: { [id: string]: boolean } = {};
  isSearching: boolean = false;
  isSkipping: boolean = false;
  downloadQueue: YouTubeItem[] = [];
  queue: YouTubeItem[] = [];

  constructor(
    private songService: SongService,
    private toastService: ToastService,
    private songState: SongStateService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (!isPlatformServer(this.platformId)) {
      // Subscribing to centralized state service
      this.songState.nowPlaying$.subscribe(item => {
        this.nowPlaying = item;
      });

      this.songState.queue$.subscribe(items => {
        this.queue = items;
        this.processQueue();
      });

      this.songState.downloadQueue$.subscribe(items => {
        this.downloadQueue = items;
        this.processQueue();
      });
    }
  }

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

  queueSong(song: YouTubeItem): void {
    this.isQueueing[song.id] = true;
    this.songService.queue(song).subscribe({
      next: () => {
        this.toastService.show('Queued successfully!');
      },
      error: () => {
        this.toastService.show('Failed to queue song.');
        this.isQueueing[song.id] = false;
      },
      complete: () => {
        this.isQueueing[song.id] = false;
      }
    });
  }

  processQueue(): void {
    this.isQueued = Object.fromEntries(
      [...this.queue, ...this.downloadQueue].map(
        (song: YouTubeItem) => [song.id, true]
      )
    );
  }

  skip(): void {
    this.isSkipping = true;
    this.songService.skip().subscribe(() => {
      this.isSkipping = false;
    });
  }

  ngOnDestroy(): void {}
}
