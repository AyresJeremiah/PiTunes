import {Component} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {SongService} from '../../services/song.service';
import {YouTubeItem} from '../../models/song.model';

@Component({
  selector: 'app-queue',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './queue.component.html',
  styleUrls: ['./queue.component.scss']
})
export class QueueComponent {
  queue: YouTubeItem[] = [];
  poolingStarted: boolean = false;

  constructor(private songService: SongService) {
  }

  refreshQueue(): void {
    this.songService.getQueue().subscribe(res => {
      this.queue = res;
    });
    this.setPooling();
  }
  setPooling(): void {
    if(!this.poolingStarted) {
      this.poolingStarted = true;
      setInterval(() => this.refreshQueue(), 5000);
    }
  }


}
