import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="badge" [ngClass]="badgeClass">
      <span class="badge-dot"></span>
      {{ label }}
    </span>
  `,
  styles: [`
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 4px 10px;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      transition: all 0.2s ease;
    }
    .badge-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
    }

    /* Track Statuses */
    .badge-draft {
      background: rgba(148, 163, 184, 0.15);
      color: #94a3b8;
      border: 1px solid rgba(148, 163, 184, 0.25);
    }
    .badge-draft .badge-dot { background: #94a3b8; }

    .badge-submitted {
      background: rgba(59, 130, 246, 0.15);
      color: #60a5fa;
      border: 1px solid rgba(59, 130, 246, 0.3);
    }
    .badge-submitted .badge-dot { background: #3b82f6; box-shadow: 0 0 6px #3b82f6; }

    .badge-distributed, .badge-live {
      background: rgba(16, 185, 129, 0.15);
      color: #34d399;
      border: 1px solid rgba(16, 185, 129, 0.3);
    }
    .badge-distributed .badge-dot, .badge-live .badge-dot {
      background: #10b981;
      box-shadow: 0 0 6px #10b981;
    }

    /* Distribution Statuses */
    .badge-pending {
      background: rgba(245, 158, 11, 0.15);
      color: #fbbf24;
      border: 1px solid rgba(245, 158, 11, 0.3);
    }
    .badge-pending .badge-dot { background: #f59e0b; box-shadow: 0 0 6px #f59e0b; }

    .badge-rejected {
      background: rgba(239, 68, 68, 0.15);
      color: #f87171;
      border: 1px solid rgba(239, 68, 68, 0.3);
    }
    .badge-rejected .badge-dot { background: #ef4444; }
  `]
})
export class StatusBadgeComponent {
  @Input() status: string = '';

  get badgeClass(): string {
    const s = this.status?.toLowerCase() || '';
    return `badge-${s}`;
  }

  get label(): string {
    return this.status || 'unknown';
  }
}
