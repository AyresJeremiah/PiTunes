import { Component, OnDestroy, OnInit, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SongService } from 'app/services/song.service';
import { YouTubeItem } from 'app/models/song.model';
import { ToastService } from 'app/services/toast.service';
import { SocketService } from 'src/app/services/socket.service';

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
  isSearching: boolean = false;
  isSkipping: boolean = false;

  constructor(
    private songService: SongService,
    private toastService: ToastService,
    private socketService: SocketService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

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
      },
      complete: () => {
        this.isQueueing[song.id] = false;
      }
    });
  }

  getNowPlaying(): void {
    this.songService.getNowPlaying()
      .subscribe((item: YouTubeItem) => { this.nowPlaying = item; });
  }

  skip(): void {
    this.isSkipping = true;
    this.songService.skip().subscribe(() => {
      this.isSkipping = false;
    });
  }

  ngOnInit(): void {
    if (!isPlatformServer(this.platformId)) {
      this.socketService.onReceiveNowPlaying((item: YouTubeItem) => {
        this.nowPlaying = item;
      });

      this.getNowPlaying();
    }
  }

  ngOnDestroy(): void {
    // cleanup if needed
  }
}
