import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  toasts: string[] = [];

  show(message: string): void {
    this.toasts.push(message);
    setTimeout(() => this.toasts.shift(), 3000); // Auto-dismiss after 3 sec
  }

  getToasts(): string[] {
    return this.toasts;
  }
}
