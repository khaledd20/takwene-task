import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Track, Dsp, TrackDistribution } from '../../models/takwene.model';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';

@Component({
  selector: 'app-track-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  template: `
    <div class="modal-backdrop" (click)="close()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <!-- Header -->
        <div class="modal-header">
          <div>
            <div class="header-pretitle">Track Details & Distribution Matrix</div>
            <h2 class="modal-title">{{ track.title }}</h2>
          </div>
          <button class="btn-close" (click)="close()">✕</button>
        </div>

        <!-- Alert messages -->
        <div *ngIf="errorMessage" class="alert alert-danger">{{ errorMessage }}</div>
        <div *ngIf="successMessage" class="alert alert-success">{{ successMessage }}</div>

        <!-- Track Metadata Grid -->
        <div class="meta-grid">
          <div class="meta-item">
            <span class="meta-label">Artist</span>
            <span class="meta-val font-highlight">{{ track.artistName }}</span>
          </div>
          <div class="meta-item">
            <span class="meta-label">ISRC Code</span>
            <span class="meta-val font-mono">{{ track.isrc }}</span>
          </div>
          <div class="meta-item">
            <span class="meta-label">Genre</span>
            <span class="meta-val">{{ track.genre }}</span>
          </div>
          <div class="meta-item">
            <span class="meta-label">Release Date</span>
            <span class="meta-val">{{ track.releaseDate | date:'mediumDate' }}</span>
          </div>
          <div class="meta-item">
            <span class="meta-label">Catalog Status</span>
            <span class="meta-val">
              <app-status-badge [status]="track.status"></app-status-badge>
            </span>
          </div>
        </div>

        <hr class="divider" />

        <!-- DSP Distribution Matrix -->
        <div class="section-title">
          <h3>DSP Distribution Status</h3>
          <span class="matrix-hint">Real-time status across major streaming platforms</span>
        </div>

        <div class="dsp-grid">
          <div *ngFor="let dsp of availableDsps" class="dsp-card" [class.dsp-active]="getDistribution(dsp.id)" [class.dsp-rejected]="getDistribution(dsp.id)?.status === 'rejected'">
            <div class="dsp-header">
              <div class="dsp-info">
                <span class="dsp-name">{{ dsp.name }}</span>
                <span class="dsp-code font-mono">{{ dsp.code }}</span>
              </div>
              <app-status-badge [status]="getDistribution(dsp.id)?.status || 'unsubmitted'"></app-status-badge>
            </div>
            <div class="dsp-body">
              <div *ngIf="getDistribution(dsp.id) as dist; else notDispatched">
                <div class="dist-timestamp">
                  <span class="time-label">Submitted:</span>
                  <span>{{ dist.submittedAt | date:'short' }}</span>
                </div>
                <div *ngIf="dist.reviewedAt" class="dist-timestamp" style="margin-top: 4px;">
                  <span class="time-label">Reviewed:</span>
                  <span>{{ dist.reviewedAt | date:'short' }}</span>
                </div>
                <div *ngIf="dist.status === 'rejected' && dist.rejectionReason" class="rejection-box">
                  <span class="rejection-label">Rejection Reason:</span>
                  <span class="rejection-text">{{ dist.rejectionReason }}</span>
                </div>

                <!-- Interactive DSP Review Simulator -->
                <div class="dsp-actions-bar">
                  <div *ngIf="rejectingDspId !== dsp.id; else rejectForm">
                    <div class="dsp-sim-btns">
                      <button
                        class="btn-sim btn-sim-live"
                        *ngIf="dist.status !== 'live'"
                        [disabled]="isUpdatingDsp"
                        (click)="onSimulateDspStatus(dsp.id, 'live')"
                        title="Simulate DSP approving and going live"
                      >
                        ✓ Set Live
                      </button>
                      <button
                        class="btn-sim btn-sim-reject"
                        *ngIf="dist.status !== 'rejected'"
                        [disabled]="isUpdatingDsp"
                        (click)="startReject(dsp.id)"
                        title="Simulate DSP rejecting this track"
                      >
                        ✕ Reject
                      </button>
                    </div>
                  </div>
                  <ng-template #rejectForm>
                    <div class="dsp-reject-input-box">
                      <input
                        type="text"
                        class="dsp-reject-input"
                        [(ngModel)]="rejectionReasonInput"
                        placeholder="Rejection reason..."
                      />
                      <div class="dsp-reject-actions">
                        <button
                          class="btn-mini btn-danger"
                          [disabled]="!rejectionReasonInput.trim() || isUpdatingDsp"
                          (click)="confirmReject(dsp.id)"
                        >
                          Confirm
                        </button>
                        <button class="btn-mini btn-secondary" (click)="cancelReject()">Cancel</button>
                      </div>
                    </div>
                  </ng-template>
                </div>
              </div>
              <ng-template #notDispatched>
                <span class="text-muted">Not dispatched yet</span>
              </ng-template>
            </div>
          </div>
        </div>

        <hr class="divider" />

        <!-- Actions Section -->
        <div class="actions-container">
          <!-- Dispatch Form -->
          <div class="action-card">
            <h4>Dispatch to DSPs</h4>
            <p class="action-desc">Select target platforms to trigger distribution (auto-transitions Draft ➔ Submitted):</p>
            
            <div class="dsp-checkbox-list">
              <label *ngFor="let dsp of availableDsps" class="dsp-checkbox-item">
                <input
                  type="checkbox"
                  [checked]="selectedDspIds.includes(dsp.id)"
                  (change)="toggleDsp(dsp.id)"
                  [disabled]="isDistributing"
                />
                <span>{{ dsp.name }}</span>
              </label>
            </div>

            <button
              class="btn btn-primary"
              [disabled]="selectedDspIds.length === 0 || isDistributing"
              (click)="onDistribute()"
            >
              {{ isDistributing ? 'Dispatching...' : 'Dispatch Selected DSPs' }}
            </button>
          </div>

          <!-- Status Updater -->
          <div class="action-card">
            <h4>Update Catalog Status</h4>
            <p class="action-desc">Direct lifecycle status change (marking Distributed cascades pending to Live):</p>
            
            <div class="status-select-row">
              <select [(ngModel)]="newStatus" class="form-select" [disabled]="isUpdatingStatus">
                <option value="draft">Draft</option>
                <option value="submitted">Submitted</option>
                <option value="distributed">Distributed</option>
              </select>

              <button
                class="btn btn-secondary"
                [disabled]="newStatus === track.status || isUpdatingStatus"
                (click)="onUpdateStatus()"
              >
                {{ isUpdatingStatus ? 'Updating...' : 'Apply Status' }}
              </button>
            </div>
          </div>
        </div>
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
      padding: 1.5rem;
      animation: fadeIn 0.2s ease;
    }
    .modal-card {
      background: #111827;
      border: 1px solid #1f2937;
      border-radius: 16px;
      max-width: 780px;
      width: 100%;
      max-height: 90vh;
      overflow-y: auto;
      padding: 2rem;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
      animation: slideUp 0.25s ease;
    }
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1.5rem;
    }
    .header-pretitle {
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      color: #6366f1;
      font-weight: 700;
      margin-bottom: 4px;
    }
    .modal-title {
      font-size: 1.75rem;
      font-weight: 800;
      color: #f3f4f6;
      margin: 0;
    }
    .btn-close {
      background: #1f2937;
      border: none;
      color: #9ca3af;
      width: 36px;
      height: 36px;
      border-radius: 50%;
      font-size: 1.1rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
    }
    .btn-close:hover {
      background: #374151;
      color: #fff;
    }
    .meta-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(130px, 1fr));
      gap: 1rem;
      background: #1a2234;
      padding: 1.25rem;
      border-radius: 12px;
      border: 1px solid #243048;
    }
    .meta-item {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .meta-label {
      font-size: 0.72rem;
      color: #9ca3af;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .meta-val {
      font-size: 0.95rem;
      color: #e5e7eb;
      font-weight: 600;
    }
    .font-highlight { color: #a5b4fc; }
    .font-mono { font-family: monospace; font-size: 0.9rem; }
    .divider {
      border: none;
      border-top: 1px solid #1f2937;
      margin: 1.75rem 0;
    }
    .section-title {
      margin-bottom: 1rem;
    }
    .section-title h3 {
      font-size: 1.15rem;
      font-weight: 700;
      color: #f3f4f6;
      margin: 0 0 4px 0;
    }
    .matrix-hint {
      font-size: 0.8rem;
      color: #9ca3af;
    }
    .dsp-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
      gap: 1rem;
      margin-top: 0.75rem;
    }
    .dsp-card {
      background: #182030;
      border: 1px solid #26334a;
      border-radius: 12px;
      padding: 1.1rem;
      transition: transform 0.2s, border-color 0.2s;
    }
    .dsp-card.dsp-active {
      border-color: #3b82f6;
      background: #19253c;
    }
    .dsp-card.dsp-rejected {
      border-color: rgba(239, 68, 68, 0.6);
      background: #23161c;
    }
    .rejection-box {
      margin-top: 6px;
      padding: 6px 8px;
      background: rgba(239, 68, 68, 0.12);
      border-left: 3px solid #ef4444;
      border-radius: 4px;
      font-size: 0.75rem;
      color: #fca5a5;
    }
    .rejection-label {
      font-weight: 700;
      display: block;
      font-size: 0.68rem;
      text-transform: uppercase;
      color: #f87171;
    }
    .rejection-text {
      word-break: break-word;
    }
    .dsp-actions-bar {
      margin-top: 8px;
      padding-top: 6px;
      border-top: 1px solid rgba(255, 255, 255, 0.08);
    }
    .dsp-sim-btns {
      display: flex;
      gap: 6px;
    }
    .btn-sim {
      flex: 1;
      padding: 4px 8px;
      font-size: 0.72rem;
      font-weight: 600;
      border-radius: 6px;
      border: 1px solid;
      cursor: pointer;
      transition: all 0.2s;
    }
    .btn-sim-live {
      background: rgba(16, 185, 129, 0.12);
      color: #34d399;
      border-color: rgba(16, 185, 129, 0.3);
    }
    .btn-sim-live:hover:not(:disabled) {
      background: rgba(16, 185, 129, 0.25);
      color: #6ee7b7;
    }
    .btn-sim-reject {
      background: rgba(239, 68, 68, 0.12);
      color: #f87171;
      border-color: rgba(239, 68, 68, 0.3);
    }
    .btn-sim-reject:hover:not(:disabled) {
      background: rgba(239, 68, 68, 0.25);
      color: #fca5a5;
    }
    .dsp-reject-input-box {
      display: flex;
      flex-direction: column;
      gap: 4px;
      margin-top: 4px;
    }
    .dsp-reject-input {
      background: #0f172a;
      border: 1px solid #374151;
      color: #f3f4f6;
      font-size: 0.75rem;
      border-radius: 4px;
      padding: 4px 6px;
      width: 100%;
      box-sizing: border-box;
    }
    .dsp-reject-actions {
      display: flex;
      gap: 4px;
      justify-content: flex-end;
    }
    .btn-mini {
      padding: 2px 8px;
      font-size: 0.7rem;
      border-radius: 4px;
      border: none;
      cursor: pointer;
    }
    .btn-mini.btn-danger {
      background: #dc2626;
      color: white;
    }
    .btn-mini.btn-secondary {
      background: #374151;
      color: #d1d5db;
    }
    .dsp-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 0.75rem;
    }
    .dsp-info {
      display: flex;
      flex-direction: column;
    }
    .dsp-name {
      font-weight: 700;
      color: #f3f4f6;
      font-size: 1rem;
    }
    .dsp-code {
      font-size: 0.75rem;
      color: #6b7280;
    }
    .dsp-body {
      font-size: 0.82rem;
    }
    .dist-timestamp {
      display: flex;
      flex-direction: column;
      gap: 2px;
      color: #cbd5e1;
    }
    .time-label {
      font-size: 0.7rem;
      color: #94a3b8;
    }
    .text-muted {
      color: #64748b;
      font-style: italic;
    }
    .actions-container {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
    }
    @media (max-width: 680px) {
      .actions-container { grid-template-columns: 1fr; }
    }
    .action-card {
      background: #161e2e;
      border: 1px solid #222e44;
      border-radius: 12px;
      padding: 1.25rem;
    }
    .action-card h4 {
      font-size: 0.95rem;
      font-weight: 700;
      color: #f3f4f6;
      margin: 0 0 6px 0;
    }
    .action-desc {
      font-size: 0.78rem;
      color: #9ca3af;
      margin: 0 0 1rem 0;
      line-height: 1.4;
    }
    .dsp-checkbox-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-bottom: 1rem;
    }
    .dsp-checkbox-item {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 0.88rem;
      color: #e5e7eb;
      cursor: pointer;
    }
    .status-select-row {
      display: flex;
      gap: 8px;
      align-items: center;
    }
    .form-select {
      background: #0f172a;
      border: 1px solid #334155;
      color: #f8fafc;
      padding: 0.55rem 0.75rem;
      border-radius: 8px;
      font-size: 0.88rem;
      flex: 1;
      outline: none;
    }
    .btn {
      padding: 0.55rem 1rem;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 600;
      cursor: pointer;
      border: none;
      transition: all 0.2s ease;
    }
    .btn-primary {
      background: #6366f1;
      color: #ffffff;
      width: 100%;
    }
    .btn-primary:hover:not(:disabled) {
      background: #4f46e5;
    }
    .btn-secondary {
      background: #334155;
      color: #f8fafc;
    }
    .btn-secondary:hover:not(:disabled) {
      background: #475569;
    }
    .btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .alert {
      padding: 0.75rem 1rem;
      border-radius: 8px;
      font-size: 0.85rem;
      margin-bottom: 1rem;
    }
    .alert-danger {
      background: rgba(239, 68, 68, 0.15);
      color: #fca5a5;
      border: 1px solid rgba(239, 68, 68, 0.3);
    }
    .alert-success {
      background: rgba(16, 185, 129, 0.15);
      color: #6ee7b7;
      border: 1px solid rgba(16, 185, 129, 0.3);
    }
    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes slideUp {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class TrackDetailComponent implements OnInit {
  @Input({ required: true }) track!: Track;
  @Input() availableDsps: Dsp[] = [];
  @Output() closeEvent = new EventEmitter<void>();
  @Output() trackUpdated = new EventEmitter<Track>();

  selectedDspIds: string[] = [];
  newStatus: 'draft' | 'submitted' | 'distributed' = 'draft';
  isDistributing = false;
  isUpdatingStatus = false;
  isUpdatingDsp = false;
  rejectingDspId: string | null = null;
  rejectionReasonInput = '';
  errorMessage = '';
  successMessage = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.newStatus = this.track.status;
    this.refreshTrack();
  }

  getDistribution(dspId: string): TrackDistribution | undefined {
    return this.track.distributions?.find(d => d.dspId === dspId);
  }

  toggleDsp(dspId: string): void {
    if (this.selectedDspIds.includes(dspId)) {
      this.selectedDspIds = this.selectedDspIds.filter(id => id !== dspId);
    } else {
      this.selectedDspIds.push(dspId);
    }
  }

  refreshTrack(): void {
    this.api.getTrackById(this.track.id).subscribe({
      next: (t) => {
        this.track = t;
        this.newStatus = t.status;
      },
      error: (err) => console.error(err)
    });
  }

  onDistribute(): void {
    this.isDistributing = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.distributeTrack(this.track.id, { dspIds: this.selectedDspIds }).subscribe({
      next: (updated) => {
        this.track = updated;
        this.newStatus = updated.status;
        this.selectedDspIds = [];
        this.isDistributing = false;
        this.successMessage = `Successfully dispatched to DSPs. Track status transitioned to ${updated.status}.`;
        this.trackUpdated.emit(updated);
      },
      error: (err) => {
        this.isDistributing = false;
        this.errorMessage = err.error?.detail || err.error?.title || 'Distribution failed. Please check your credentials.';
      }
    });
  }

  onUpdateStatus(): void {
    this.isUpdatingStatus = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.updateTrackStatus(this.track.id, { status: this.newStatus }).subscribe({
      next: (updated) => {
        this.track = updated;
        this.isUpdatingStatus = false;
        this.successMessage = `Track status updated to ${updated.status}. DSP distributions updated accordingly.`;
        this.trackUpdated.emit(updated);
      },
      error: (err) => {
        this.isUpdatingStatus = false;
        this.errorMessage = err.error?.detail || err.error?.title || 'Failed to update track status.';
      }
    });
  }

  close(): void {
    this.closeEvent.emit();
  }

  startReject(dspId: string): void {
    this.rejectingDspId = dspId;
    this.rejectionReasonInput = 'Metadata does not match audio recording standards.';
  }

  cancelReject(): void {
    this.rejectingDspId = null;
    this.rejectionReasonInput = '';
  }

  confirmReject(dspId: string): void {
    if (!this.rejectionReasonInput.trim()) return;
    this.onSimulateDspStatus(dspId, 'rejected', this.rejectionReasonInput.trim());
    this.rejectingDspId = null;
  }

  onSimulateDspStatus(dspId: string, status: 'live' | 'rejected', reason?: string): void {
    this.isUpdatingDsp = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.updateDistributionStatus(this.track.id, dspId, {
      status,
      rejectionReason: reason
    }).subscribe({
      next: (updated) => {
        this.track = updated;
        this.newStatus = updated.status;
        this.isUpdatingDsp = false;
        this.successMessage = `DSP status successfully updated to ${status}.`;
        this.trackUpdated.emit(updated);
      },
      error: (err) => {
        this.isUpdatingDsp = false;
        this.errorMessage = err.error?.detail || err.error?.title || 'Failed to update DSP distribution status.';
      }
    });
  }
}
