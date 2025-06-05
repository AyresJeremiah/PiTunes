import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { YouTubeItem } from '../../models/song.model';
import { SocketService } from '../../services/socket.service';

@Component({
  selector: 'app-queue',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './queue.component.html',
  styleUrls: ['./queue.component.scss']
})
export class QueueComponent implements OnInit, OnDestroy {
  queue: YouTubeItem[] = [];

  constructor(private socketService: SocketService) {}

  ngOnInit(): void {
    // Subscribe to ReceiveQueue event
    this.socketService.onReceiveQueue((items: YouTubeItem[]) => {
      this.queue = items;
    });
  }

  ngOnDestroy(): void {
    // Optional: If you're shutting down app-level socket on component destroy
    // In most apps you actually leave socketService running globally
    // this.socketService.stop();
  }
}
