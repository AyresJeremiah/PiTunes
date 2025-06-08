import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { YouTubeItem } from 'app/models/song.model';
import { SongService } from 'src/app/services/song.service';
import { ToastService } from 'src/app/services/toast.service';
import {SongStateService} from 'src/app/services/song.state.service';

@Component({
  selector: 'app-song-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './song-management.component.html',
  styleUrls: ['./song-management.component.scss']
})
export class SongManagementComponent implements OnInit, OnDestroy {
  songs: YouTubeItem[] = [];
  downloadQueue: YouTubeItem[] = [];
  queue: YouTubeItem[] = [];
  isQueueing: { [id: string]: boolean } = {};
  isQueued: { [id: string]: boolean } = {};
  nowPlaying: YouTubeItem | null = null;
  isDeleting: { [id: string]: boolean } = {};

  constructor(
    private toastService: ToastService,
    private songService: SongService,
    private songState: SongStateService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (!isPlatformServer(this.platformId)) {
      this.songState.nowPlaying$.subscribe(item => {
        this.nowPlaying = item;
        this.processQueue();
      });

      this.songState.queue$.subscribe(items => {
        this.queue = items;
        this.processQueue();
      });

      this.songState.downloadQueue$.subscribe(items => {
        this.downloadQueue = items;
        this.processQueue();
      });

      this.songState.songs$.subscribe(items => {
        this.songs = items;
      });
    }
  }

  processQueue(): void {
    this.isQueued = Object.fromEntries(
      [...this.queue, ...this.downloadQueue].map(
        (song: YouTubeItem) => [song.id, true]
      )
    );
    if (this.nowPlaying !== null) {
      this.isQueued[this.nowPlaying.id] = true;
    }
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

  deleteSong(song: YouTubeItem): void {
    this.isQueueing[song.id] = true;
    this.songService.delete(song).subscribe({
      next: () => {
        this.toastService.show('Deleted successfully!');
      },
      error: () => {
        this.toastService.show('Failed to queue song.');
      }
    });
  }

  ngOnDestroy(): void {}
}
