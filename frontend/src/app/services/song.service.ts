import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { YouTubeSearchResult } from '../models/song.model';

@Injectable({
  providedIn: 'root'
})
export class SongService {
  constructor(private http: HttpClient) {}

  search(query: string): Observable<YouTubeSearchResult[]> {
    return this.http.get<YouTubeSearchResult[]>(`/api/songs/search`, { params: { query } });
  }

  queue(song: YouTubeSearchResult): Observable<any> {
    return this.http.post('/api/songs/queue', song);
  }

  getQueue() {
    return this.http.get<YouTubeSearchResult[]>('/api/songs/queue');
  }

  getNowPlaying() {
    return this.http.get<YouTubeSearchResult>('/api/songs/now-playing');
  }

  skip(): Observable<any> {
    return this.http.post('/api/songs/skip', {});
  }

}
