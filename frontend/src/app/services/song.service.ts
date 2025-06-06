import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { YouTubeItem } from '../models/song.model';

@Injectable({
  providedIn: 'root'
})
export class SongService {
  constructor(private http: HttpClient) {}

  search(query: string): Observable<YouTubeItem[]> {
    return this.http.get<YouTubeItem[]>(`/api/songs/search`, { params: { query } });
  }

  queue(song: YouTubeItem): Observable<any> {
    return this.http.post('/api/songs/queue', song);
  }

  getQueue() {
    return this.http.get<YouTubeItem[]>('/api/songs/queue');
  }

  getDownloadQueue() {
    return this.http.get<YouTubeItem[]>('/api/songs/download-queue');
  }

  getNowPlaying() {
    return this.http.get<YouTubeItem>('/api/songs/now-playing');
  }

  skip(): Observable<any> {
    return this.http.post('/api/songs/skip', {});
  }

}
