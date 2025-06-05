import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import {YouTubeItem} from 'src/app/models/song.model';

@Injectable({
  providedIn: 'root'
})
export class SocketService {

  private hubConnection: signalR.HubConnection;

  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:4200/api/hubs/socket")
      .withAutomaticReconnect()
      .build();
  }

  public start(): void {
    this.hubConnection
      .start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error('Error connecting to SignalR: ', err));
  }

  // Listen for full queue updates
  public onReceiveQueue(callback: (items: YouTubeItem[]) => void): void {
    this.hubConnection.on("ReceiveQueue", (items: YouTubeItem[]) => {
      callback(items);
    });
  }

  // Listen for NowPlaying updates
  public onReceiveNowPlaying(callback: (item: YouTubeItem) => void): void {
    this.hubConnection.on("ReceiveNowPlaying", (item: YouTubeItem) => {
      callback(item);
    });
  }

  public stop(): void {
    this.hubConnection.stop()
      .then(() => console.log("SignalR disconnected"))
      .catch(err => console.error("Error disconnecting: ", err));
  }
}
