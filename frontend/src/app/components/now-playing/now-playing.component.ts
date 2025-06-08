import {Component, OnInit, OnDestroy, Inject, PLATFORM_ID} from '@angular/core';
import {isPlatformServer, CommonModule} from '@angular/common';
import {SongService} from 'app/services/song.service';
import {YouTubeItem} from 'app/models/song.model';
import {SongStateService} from 'src/app/services/song.state.service';

@Component({
  selector: 'app-now-playing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './now-playing.component.html',
  styleUrls: ['./now-playing.component.scss'],
})
export class NowPlayingComponent implements OnInit, OnDestroy {
  query: string = '';
  nowPlaying: YouTubeItem | null = null;
  isSkipping: boolean = false;
  queue: YouTubeItem[] = [];

  constructor(
    private songService: SongService,
    private songState: SongStateService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
  }

  ngOnInit(): void {
    if (!isPlatformServer(this.platformId)) {
      this.songState.nowPlaying$.subscribe(item => {
        this.nowPlaying = item;
      });
    }
  }

  skip(): void {
    this.isSkipping = true;
    this.songService.skip().subscribe(() => {
      this.isSkipping = false;
    });
  }

  ngOnDestroy(): void {
  }
}
