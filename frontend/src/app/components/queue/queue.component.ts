import {Component} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {SongService} from '../../services/song.service';
import {YouTubeSearchResult} from '../../models/song.model';
import {ToastService} from 'app/services/toast.service';
import {OnInit, OnDestroy} from '@angular/core';
import {interval, Observable, Subject, Subscription, switchMap, takeUntil, timer} from 'rxjs';

@Component({
  selector: 'app-queue',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './queue.component.html',
  styleUrls: ['./queue.component.scss']
})
export class QueueComponent {
  queue: YouTubeSearchResult[] = [];
  poolingStarted: boolean = false;
  private intervalId: any;
  private subscription!: Subscription;
  closeTimer$ = new Subject<any>();

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
