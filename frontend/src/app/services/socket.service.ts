import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import * as signalR from '@microsoft/signalr';
import { YouTubeItem } from 'src/app/models/song.model';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SocketService {

  private hubConnection: signalR.HubConnection | null = null;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    // DO NOT initialize hubConnection in constructor to prevent SSR errors.
  }

  public start(): void {
    if (isPlatformServer(this.platformId)) {
      console.log("Skipping SignalR connection during SSR");
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`/api/hubs/socket`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error('Error connecting to SignalR: ', err));

    document.addEventListener("visibilitychange", () => {
      if (document.visibilityState === 'visible') {
        location.reload();
      }
    });
  }

  public onReceiveQueue(callback: (items: YouTubeItem[]) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on("ReceiveQueue", callback);
    }
  }

  public onReceiveDownloadQueue(callback: (items: YouTubeItem[]) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on("ReceiveDownloadQueue", callback);
    }
  }

  public onReceiveDeletedSongFromCache(callback: (item: YouTubeItem) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on("ReceiveDeletedSongFromCache", callback);
    }
  }

  public onReceiveDownloadedSong(callback: (item: YouTubeItem) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on("ReceiveDownloadedSong", callback);
    }
  }

  public onReceiveNowPlaying(callback: (item: YouTubeItem) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on("ReceiveNowPlaying", callback);
    }
  }

  public stop(): void {
    if (this.hubConnection) {
      this.hubConnection.stop()
        .then(() => console.log('SignalR connection stopped'))
        .catch(err => console.error('Error stopping SignalR connection:', err));
      this.hubConnection = null;
    }
  }



}
