import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { YouTubeItem } from 'app/models/song.model';
import { SongService } from 'src/app/services/song.service';
import {SongStateService} from 'src/app/services/song.state.service';

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
    private songService: SongService,
    private songState: SongStateService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (!isPlatformServer(this.platformId)) {
      this.songState.queue$.subscribe(items => {
        this.queue = items;
      });

      this.songState.downloadQueue$.subscribe(items => {
        this.downloadQueue = items;
      });
    }
  }

  ngOnDestroy(): void {}
}
