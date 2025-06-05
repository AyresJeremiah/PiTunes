import {Component, OnInit, Inject, PLATFORM_ID} from '@angular/core';
import {isPlatformServer} from '@angular/common';

import {SearchComponent} from './components/search/search.component';
import {ToastComponent} from 'src/app/shared/toast/toast.component';
import {FormsModule} from '@angular/forms';
import {HttpClientModule} from '@angular/common/http';
import {SlideUpDrawerComponent} from 'src/app/shared/slide-up-drawer/slide-up-drawer.component';
import {QueueComponent} from 'src/app/components/queue/queue.component';
import {SocketService} from './services/socket.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    SearchComponent,
    ToastComponent,
    FormsModule,
    HttpClientModule,
    SlideUpDrawerComponent,
    QueueComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'frontend';

  constructor(
    private socketService: SocketService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
  }

  ngOnInit(): void {
    if (!isPlatformServer(this.platformId)) {
      this.socketService.start();
    } else {
      console.log("Skipping SignalR connection during SSR prerender");
    }
  }
}
