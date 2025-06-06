import { Component, Output, EventEmitter, HostBinding } from '@angular/core';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-slide-up-drawer',
  standalone: true,
  templateUrl: './slide-up-drawer.component.html',
  imports: [NgIf],
  styleUrls: ['./slide-up-drawer.component.scss']
})
export class SlideUpDrawerComponent {
  private _isOpen: boolean = false;

  @Output() closed = new EventEmitter<void>();

  get isOpen(): boolean {
    return this._isOpen;
  }

  set isOpen(value: boolean) {
    this._isOpen = value;
    this.lockBodyScroll(value);
  }

  close() {
    this.isOpen = false;
    this.closed.emit();
  }

  private lockBodyScroll(lock: boolean) {
    document.body.style.overflow = lock ? 'hidden' : 'auto';
  }
}
