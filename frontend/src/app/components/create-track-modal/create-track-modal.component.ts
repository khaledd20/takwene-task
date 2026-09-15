import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Artist, CreateTrackRequest, Track } from '../../models/takwene.model';

@Component({
  selector: 'app-create-track-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-backdrop" (click)="close()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h2 class="modal-title">Create New Track</h2>
          <button class="btn-close" (click)="close()">✕</button>
        </div>

        <div *ngIf="errorMessage" class="alert alert-danger">{{ errorMessage }}</div>

        <form (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label class="form-label">Track Title *</label>
            <input
              type="text"
              class="form-control"
              [(ngModel)]="formData.title"
              name="title"
              placeholder="e.g. Tamally Maak"
              required
            />
          </div>

          <div class="form-group">
            <label class="form-label">Artist *</label>
            <select class="form-control" [(ngModel)]="formData.artistId" name="artistId" required>
              <option value="" disabled>Select Artist</option>
              <option *ngFor="let a of artists" [value]="a.id">{{ a.name }} ({{ a.country }})</option>
            </select>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label class="form-label">ISRC Code *</label>
              <input
                type="text"
                class="form-control font-mono"
                [(ngModel)]="formData.isrc"
                name="isrc"
                placeholder="e.g. EGG012300001"
                required
              />
              <span class="field-hint">ISO 3901 format (e.g. USRC17607839)</span>
            </div>

            <div class="form-group">
              <label class="form-label">Genre *</label>
              <input
                type="text"
                class="form-control"
                [(ngModel)]="formData.genre"
                name="genre"
                placeholder="e.g. Arabic Pop"
                required
              />
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Release Date *</label>
            <input
              type="date"
              class="form-control"
              [(ngModel)]="formData.releaseDate"
              name="releaseDate"
              required
            />
          </div>

          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" (click)="close()">Cancel</button>
            <button type="submit" class="btn btn-primary" [disabled]="isSubmitting">
              {{ isSubmitting ? 'Creating...' : 'Create Track' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.75);
      backdrop-filter: blur(8px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }
    .modal-card {
      background: #111827;
      border: 1px solid #1f2937;
      border-radius: 16px;
      max-width: 540px;
      width: 100%;
      padding: 2rem;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
    }
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
    }
    .modal-title {
      font-size: 1.4rem;
      font-weight: 700;
      color: #f3f4f6;
      margin: 0;
    }
    .btn-close {
      background: #1f2937;
      border: none;
      color: #9ca3af;
      width: 32px;
      height: 32px;
      border-radius: 50%;
      cursor: pointer;
    }
    .form-group {
      margin-bottom: 1.15rem;
    }
    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }
    .form-label {
      display: block;
      font-size: 0.8rem;
      font-weight: 600;
      color: #9ca3af;
      margin-bottom: 6px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }
    .form-control {
      width: 100%;
      background: #1e293b;
      border: 1px solid #334155;
      color: #f8fafc;
      padding: 0.65rem 0.85rem;
      border-radius: 8px;
      font-size: 0.9rem;
      outline: none;
      box-sizing: border-box;
    }
    .form-control:focus {
      border-color: #6366f1;
    }
    .font-mono { font-family: monospace; }
    .field-hint {
      font-size: 0.72rem;
      color: #64748b;
      margin-top: 4px;
      display: block;
    }
    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      margin-top: 1.75rem;
    }
    .btn {
      padding: 0.6rem 1.25rem;
      border-radius: 8px;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      border: none;
    }
    .btn-primary { background: #6366f1; color: white; }
    .btn-secondary { background: #334155; color: white; }
    .alert-danger {
      background: rgba(239, 68, 68, 0.15);
      color: #fca5a5;
      border: 1px solid rgba(239, 68, 68, 0.3);
      padding: 0.75rem;
      border-radius: 8px;
      margin-bottom: 1rem;
      font-size: 0.85rem;
    }
  `]
})
export class CreateTrackModalComponent {
  @Input() artists: Artist[] = [];
  @Output() closeEvent = new EventEmitter<void>();
  @Output() trackCreated = new EventEmitter<Track>();

  formData: CreateTrackRequest = {
    title: '',
    artistId: '',
    isrc: '',
    genre: '',
    releaseDate: new Date().toISOString().split('T')[0]
  };

  isSubmitting = false;
  errorMessage = '';

  constructor(private api: ApiService) {}

  onSubmit(): void {
    if (!this.formData.title || !this.formData.artistId || !this.formData.isrc || !this.formData.genre) {
      this.errorMessage = 'Please complete all required fields.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.api.createTrack(this.formData).subscribe({
      next: (track) => {
        this.isSubmitting = false;
        this.trackCreated.emit(track);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.detail || err.error?.title || 'Failed to create track.';
      }
    });
  }

  close(): void {
    this.closeEvent.emit();
  }
}
