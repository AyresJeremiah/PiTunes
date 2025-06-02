import { Component } from '@angular/core';
import { SearchComponent } from './components/search/search.component';
import { ToastComponent } from 'src/app/shared/toast/toast.component';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [SearchComponent, ToastComponent, FormsModule, HttpClientModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'frontend';
}
