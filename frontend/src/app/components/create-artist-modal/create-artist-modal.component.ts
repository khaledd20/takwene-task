import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Artist, CreateArtistRequest } from '../../models/takwene.model';

@Component({
  selector: 'app-create-artist-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-backdrop" (click)="close()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h2 class="modal-title">Register New Artist</h2>
          <button class="btn-close" (click)="close()">✕</button>
        </div>

        <div *ngIf="errorMessage" class="alert alert-danger">{{ errorMessage }}</div>

        <form (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label class="form-label">Artist Name *</label>
            <input
              type="text"
              class="form-control"
              [(ngModel)]="formData.name"
              name="name"
              placeholder="e.g. Amr Diab"
              required
            />
          </div>

          <div class="form-group">
            <label class="form-label">Email *</label>
            <input
              type="email"
              class="form-control"
              [(ngModel)]="formData.email"
              name="email"
              placeholder="e.g. amr@diab.com"
              required
            />
          </div>

          <div class="form-group">
            <label class="form-label">Country Code / Name *</label>
            <input
              type="text"
              class="form-control"
              [(ngModel)]="formData.country"
              name="country"
              placeholder="e.g. Egypt"
              required
            />
          </div>

          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" (click)="close()">Cancel</button>
            <button type="submit" class="btn btn-primary" [disabled]="isSubmitting">
              {{ isSubmitting ? 'Registering...' : 'Register Artist' }}
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
      max-width: 480px;
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
      font-size: 1.35rem;
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
export class CreateArtistModalComponent {
  @Output() closeEvent = new EventEmitter<void>();
  @Output() artistCreated = new EventEmitter<Artist>();

  formData: CreateArtistRequest = {
    name: '',
    email: '',
    country: ''
  };

  isSubmitting = false;
  errorMessage = '';

  constructor(private api: ApiService) {}

  onSubmit(): void {
    if (!this.formData.name || !this.formData.email || !this.formData.country) {
      this.errorMessage = 'All fields are required.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.api.createArtist(this.formData).subscribe({
      next: (artist) => {
        this.isSubmitting = false;
        this.artistCreated.emit(artist);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.detail || err.error?.title || 'Failed to create artist.';
      }
    });
  }

  close(): void {
    this.closeEvent.emit();
  }
}
