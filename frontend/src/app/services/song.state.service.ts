import {Injectable} from '@angular/core';
import {BehaviorSubject} from 'rxjs';
import {SongService} from './song.service';
import {SocketService} from './socket.service';
import {YouTubeItem} from 'src/app/models/song.model';

@Injectable({
  providedIn: 'root'
})
export class SongStateService {

  private nowPlayingSubject = new BehaviorSubject<YouTubeItem | null>(null);
  private queueSubject = new BehaviorSubject<YouTubeItem[]>([]);
  private downloadQueueSubject = new BehaviorSubject<YouTubeItem[]>([]);
  private songsSubject = new BehaviorSubject<YouTubeItem[]>([]);

  nowPlaying$ = this.nowPlayingSubject.asObservable();
  queue$ = this.queueSubject.asObservable();
  downloadQueue$ = this.downloadQueueSubject.asObservable();
  songs$ = this.songsSubject.asObservable();

  constructor(
    private songService: SongService,
    private socketService: SocketService
  ) {
  }

  initialize() {
    // Load initial data via HTTP
    this.songService.getNowPlaying().subscribe(item => {
      this.nowPlayingSubject.next(item);
    });

    this.songService.getQueue().subscribe(items => {
      this.queueSubject.next(items);
    });

    this.songService.getDownloadQueue().subscribe(items => {
      this.downloadQueueSubject.next(items);
    });

    this.songService.getSongs().subscribe(items => {
      this.songsSubject.next(items);
    });

    // Hook up SignalR events
    this.socketService.onReceiveQueue((items: YouTubeItem[]) => {
      this.queueSubject.next(items);
    });

    this.socketService.onReceiveDownloadQueue((items: YouTubeItem[]) => {
      this.downloadQueueSubject.next(items);
    });

    this.socketService.onReceiveNowPlaying((item: YouTubeItem) => {
      this.nowPlayingSubject.next(item);
    });

    this.socketService.onReceiveDownloadedSong((item: YouTubeItem) => {
      const currentSongs = this.songsSubject.value ?? [];

      const exists = currentSongs.some(s => s.id === item.id);
      if (!exists) {
        currentSongs.push(item);
        this.songsSubject.next(currentSongs);
      }
    });


    this.socketService.onReceiveDeletedSongFromCache((item: YouTubeItem) => {
      const updatedSongs = this.songsSubject.value.filter(s => s.id !== item.id);
      this.songsSubject.next(updatedSongs);
    });
  }

  // Optional manual refresh if needed
  refreshAll() {
    this.songService.getNowPlaying().subscribe(item => this.nowPlayingSubject.next(item));
    this.songService.getQueue().subscribe(items => this.queueSubject.next(items));
    this.songService.getDownloadQueue().subscribe(items => this.downloadQueueSubject.next(items));
    this.songService.getSongs().subscribe(items => this.songsSubject.next(items));
  }
}
