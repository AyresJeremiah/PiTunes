import {Component, OnInit, Inject, PLATFORM_ID} from '@angular/core';
import {isPlatformServer} from '@angular/common';

import {ToastComponent} from 'src/app/shared/toast/toast.component';
import {FormsModule} from '@angular/forms';
import {HttpClientModule} from '@angular/common/http';
import {SlideUpDrawerComponent} from 'src/app/shared/slide-up-drawer/slide-up-drawer.component';
import {QueueComponent} from 'src/app/components/queue/queue.component';
import {SocketService} from './services/socket.service';
import {BottomTrayComponent} from 'src/app/components/bottom-tray/bottom-tray.component';
import {SongManagementComponent} from 'src/app/components/song-management/song-management.component';
import {SongStateService} from 'src/app/services/song.state.service';
import {SearchComponent} from 'src/app/components/search/search.component';
import {NowPlayingComponent} from 'src/app/components/now-playing/now-playing.component';
import {AiSuggestComponent} from 'src/app/components/ai-suggest/ai-suggest.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    SearchComponent,
    ToastComponent,
    FormsModule,
    HttpClientModule,
    SlideUpDrawerComponent,
    QueueComponent,
    BottomTrayComponent,
    BottomTrayComponent,
    SongManagementComponent,
    SearchComponent,
    SearchComponent,
    SearchComponent,
    SearchComponent,
    NowPlayingComponent,
    AiSuggestComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'frontend';

  constructor(
    private socketService: SocketService,
    private songStateService: SongStateService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
  }

  ngOnInit(): void {
    if (!isPlatformServer(this.platformId)) {
      this.socketService.start();
      this.songStateService.initialize();
    } else {
      console.log("Skipping SignalR connection during SSR prerender");
    }
  }
}
