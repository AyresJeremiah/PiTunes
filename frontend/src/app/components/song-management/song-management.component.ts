import {Component, OnInit, OnDestroy, Inject, PLATFORM_ID} from '@angular/core';
import {isPlatformServer} from '@angular/common';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {YouTubeItem} from 'app/models/song.model';
import {SocketService} from 'app/services/socket.service';
import {SongService} from 'src/app/services/song.service';
import {ToastService} from 'src/app/services/toast.service';

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

  constructor(
    private socketService: SocketService,
    private toastService: ToastService,
    private songService: SongService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
  }

  getSongs(): void {
    this.songService.getSongs()
      .subscribe((items: YouTubeItem[]) => {
        this.songs = items;
      });
  }

  getData(): void {
    this.songService.getNowPlaying()
      .subscribe((item: YouTubeItem) => {
        this.nowPlaying = item;
      });
    this.songService.getQueue()
      .subscribe((items: YouTubeItem[]) => {
        this.queue = items;
        this.processQueue();
      });
    this.songService.getDownloadQueue()
      .subscribe((items: YouTubeItem[]) => {
        this.downloadQueue = items;
        this.processQueue();
      });
  }

  processQueue(): void {
    this.isQueued = Object.fromEntries(
      [...this.queue, ...this.downloadQueue].map(
        (song: YouTubeItem) => [song.id, true]
      )
    );
    if(this.nowPlaying !== null) {
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

  ngOnInit(): void {
    this.socketService.onReceiveQueue((items: YouTubeItem[]) => {
      this.queue = items;
    });
    this.socketService.onReceiveDownloadQueue((items: YouTubeItem[]) => {
      this.downloadQueue = items;
    });
    this.socketService.onReceiveNowPlaying((item: YouTubeItem) => {
      this.nowPlaying = item;
    });

    this.getSongs();
    this.getData();
  }

  ngOnDestroy(): void {
    // no-op for now
  }
}
