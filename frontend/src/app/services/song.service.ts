import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { YouTubeItem } from '../models/song.model';
import {SuggestRequest} from 'src/app/models/suggestRequest.model';

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

  dequeue(song: YouTubeItem): Observable<any> {
    return this.http.post('/api/songs/dequeue', song);
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

  getSongs() {
    return this.http.get<YouTubeItem[]>('/api/songs/downloaded');
  }

  delete(song: YouTubeItem): Observable<any> {
    return this.http.post('/api/songs/delete', song);
  }

  aiSuggest(request: SuggestRequest): Observable<any> {
    return this.http.post('/api/songs/suggest', request);
  }
}
