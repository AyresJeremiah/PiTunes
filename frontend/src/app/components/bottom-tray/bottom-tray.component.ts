import {Component, ContentChildren, QueryList, AfterContentInit, Inject, PLATFORM_ID} from '@angular/core';
import { SlideUpDrawerComponent } from 'src/app/shared/slide-up-drawer/slide-up-drawer.component';
import {SocketService} from 'src/app/services/socket.service';
import {SongStateService} from 'src/app/services/song.state.service';
import {ConfigService} from 'src/app/services/config.service';
import {NgIf} from '@angular/common';

@Component({
  selector: 'app-bottom-tray',
  standalone: true,
  templateUrl: './bottom-tray.component.html',
  imports: [
    NgIf
  ],
  styleUrls: ['./bottom-tray.component.scss']
})
export class BottomTrayComponent implements AfterContentInit {
  @ContentChildren(SlideUpDrawerComponent) drawers!: QueryList<SlideUpDrawerComponent>;

  aiEnabled: boolean = false;

  constructor(
    private configService: ConfigService,
  ) {
  }
  ngAfterContentInit() {
    console.log('Drawers found:', this.drawers.length);
  }

  toggleDrawer(index: number) {
    const drawer = this.drawers.toArray()[index];

    if (!drawer) return;

    if (drawer.isOpen) {
      drawer.isOpen = false;
    } else {
      this.closeAllDrawers();
      drawer.isOpen = true;
    }
  }

  ngOnInit(){
    this.configService.getConfig().subscribe({
      next: config => {
        this.aiEnabled = config.aiEnabled;
      },
      error: err => {
        console.error('Failed to load configuration:', err);
      }
    });
  }

  closeAllDrawers() {
    this.drawers.forEach(drawer => drawer.isOpen = false);
  }
}
