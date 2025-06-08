import {Component} from '@angular/core';
import {SongService} from 'src/app/services/song.service';
import {FormsModule} from '@angular/forms';

interface SongSuggestion {
  title: string;
  artist: string;
}

@Component({
  selector: 'app-ai-suggest',
  imports: [
    FormsModule
  ],
  templateUrl: './ai-suggest.component.html',
  styleUrl: './ai-suggest.component.scss'
})

export class AiSuggestComponent {
  prompt: string = '';
  suggestions: SongSuggestion[] = [];
  loading: boolean = false;
  error: string = '';

  constructor(
    private songService: SongService,
  ) {
  }

  submitPrompt() {
    if (!this.prompt) return;

    this.loading = true;
    this.error = '';
    this.suggestions = [];

    this.songService.aiSuggest({prompt: this.prompt}).subscribe({
      next: res => {
        this.suggestions = res;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to get suggestions';
        this.loading = false;
      }
    });

  }
}
