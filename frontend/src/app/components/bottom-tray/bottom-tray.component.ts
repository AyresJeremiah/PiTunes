import { Component, ContentChildren, QueryList, AfterContentInit } from '@angular/core';
import { SlideUpDrawerComponent } from 'src/app/shared/slide-up-drawer/slide-up-drawer.component';

@Component({
  selector: 'app-bottom-tray',
  standalone: true,
  templateUrl: './bottom-tray.component.html',
  styleUrls: ['./bottom-tray.component.scss']
})
export class BottomTrayComponent implements AfterContentInit {
  @ContentChildren(SlideUpDrawerComponent) drawers!: QueryList<SlideUpDrawerComponent>;

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


  closeAllDrawers() {
    this.drawers.forEach(drawer => drawer.isOpen = false);
  }
}
