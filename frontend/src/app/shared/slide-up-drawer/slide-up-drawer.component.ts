import { Component } from '@angular/core';
import { NgIf } from '@angular/common';
@Component({
  selector: 'app-slide-up-drawer',
  templateUrl: './slide-up-drawer.component.html',
  imports: [
    NgIf
  ],
  styleUrls: ['./slide-up-drawer.component.scss']
})
export class SlideUpDrawerComponent {
  isOpen: boolean = false;

  toggle() {
    this.isOpen = !this.isOpen;
  }

  close() {
    this.isOpen = false;
  }

}
