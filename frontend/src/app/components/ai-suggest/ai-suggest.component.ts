import {Component} from '@angular/core';
import {SongService} from 'src/app/services/song.service';
import {FormsModule} from '@angular/forms';
import {CommonModule} from '@angular/common';
import {AiSongSuggestion} from 'src/app/models/aiSongSuggestion.model';
import {YouTubeItem} from 'src/app/models/song.model';
import {ToastService} from 'src/app/services/toast.service';

@Component({
  selector: 'app-ai-suggest',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './ai-suggest.component.html',
  styleUrl: './ai-suggest.component.scss'
})

export class AiSuggestComponent {
  prompt: string = '';
  suggestions: AiSongSuggestion[] = [];
  loading: boolean = false;
  error: string = '';

  constructor(
    private songService: SongService,
    private toastService: ToastService
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
  queueSong(song: AiSongSuggestion): void {
    this.songService.queueAiSong(song).subscribe({
      next: () => {
        this.toastService.show('Queued successfully!');
      },
      error: () => {
        this.toastService.show('Failed to queue song.');
      },
      complete: () => {
      }
    });
  }
}
