import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { YouTubeItem } from 'app/models/song.model';
import { SocketService } from 'app/services/socket.service';
import { SongService } from 'src/app/services/song.service';

@Component({
  selector: 'app-queue',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './queue.component.html',
  styleUrls: ['./queue.component.scss']
})
export class QueueComponent implements OnInit, OnDestroy {
  queue: YouTubeItem[] = [];
  downloadQueue: YouTubeItem[] = [];

  constructor(
    private socketService: SocketService,
    private songService: SongService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  getQueue(): void {
    this.songService.getQueue()
      .subscribe((items: YouTubeItem[]) => { this.queue = items; });
  }

  ngOnInit(): void {
    if (!isPlatformServer(this.platformId)) {
      this.socketService.onReceiveQueue((items: YouTubeItem[]) => {
        this.queue = items;
      });

      this.socketService.onReceiveDownloadQueue((items: YouTubeItem[]) => {
        this.downloadQueue = items;
      });

      this.getQueue();
    }
  }

  ngOnDestroy(): void {
    // no-op for now
  }
}
