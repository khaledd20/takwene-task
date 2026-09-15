import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from './services/api.service';
import { Track, Artist, Dsp } from './models/takwene.model';
import { StatusBadgeComponent } from './components/status-badge/status-badge.component';
import { TrackDetailComponent } from './components/track-detail/track-detail.component';
import { CreateTrackModalComponent } from './components/create-track-modal/create-track-modal.component';
import { CreateArtistModalComponent } from './components/create-artist-modal/create-artist-modal.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    StatusBadgeComponent,
    TrackDetailComponent,
    CreateTrackModalComponent,
    CreateArtistModalComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  tracks: Track[] = [];
  artists: Artist[] = [];
  dsps: Dsp[] = [];

  selectedStatus: string = 'all';
  searchQuery: string = '';

  selectedTrack: Track | null = null;
  showTrackModal = false;
  showArtistModal = false;

  isLoading = true;
  isLoggingIn = false;
  toastMessage = '';

  constructor(public api: ApiService) {}

  ngOnInit(): void {
    this.loadInitialData();
  }

  loadInitialData(): void {
    this.isLoading = true;
    this.api.getTracks().subscribe({
      next: (tracks) => {
        this.tracks = tracks;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading tracks:', err);
        this.isLoading = false;
      }
    });

    this.api.getArtists().subscribe({
      next: (artists) => (this.artists = artists),
      error: (err) => console.error('Error loading artists:', err)
    });

    this.api.getDsps().subscribe({
      next: (dsps) => (this.dsps = dsps),
      error: (err) => console.error('Error loading dsps:', err)
    });
  }

  get filteredTracks(): Track[] {
    return this.tracks.filter((track) => {
      const matchStatus =
        this.selectedStatus === 'all' ||
        track.status.toLowerCase() === this.selectedStatus.toLowerCase();

      const q = this.searchQuery.trim().toLowerCase();
      const matchSearch =
        !q ||
        track.title.toLowerCase().includes(q) ||
        track.artistName.toLowerCase().includes(q) ||
        track.isrc.toLowerCase().includes(q) ||
        track.genre.toLowerCase().includes(q);

      return matchStatus && matchSearch;
    });
  }

  setStatusFilter(status: string): void {
    this.selectedStatus = status;
  }

  countByStatus(status: string): number {
    return this.tracks.filter((t) => t.status.toLowerCase() === status.toLowerCase()).length;
  }

  openTrackDetail(track: Track): void {
    this.api.getTrackById(track.id).subscribe({
      next: (fullTrack) => {
        this.selectedTrack = fullTrack;
      },
      error: () => {
        this.selectedTrack = track;
      }
    });
  }

  onTrackUpdated(updated: Track): void {
    const idx = this.tracks.findIndex((t) => t.id === updated.id);
    if (idx !== -1) {
      this.tracks[idx] = updated;
    }
    this.showToast(`Track "${updated.title}" updated successfully.`);
  }

  onTrackCreated(newTrack: Track): void {
    this.tracks.unshift(newTrack);
    this.showTrackModal = false;
    this.showToast(`Track "${newTrack.title}" registered in catalog.`);
  }

  onArtistCreated(newArtist: Artist): void {
    this.artists.push(newArtist);
    this.showArtistModal = false;
    this.showToast(`Artist "${newArtist.name}" registered.`);
  }

  loginDemo(): void {
    this.isLoggingIn = true;
    this.api.login('admin@takwene.com', 'Password123!').subscribe({
      next: (res) => {
        this.isLoggingIn = false;
        this.showToast(`Authenticated as ${res.email} (JWT Token saved)`);
      },
      error: (err) => {
        this.isLoggingIn = false;
        this.showToast('Login failed. Ensure backend API is running.');
      }
    });
  }

  logout(): void {
    this.api.logout();
    this.showToast('Logged out successfully.');
  }

  private showToast(msg: string): void {
    this.toastMessage = msg;
    setTimeout(() => {
      if (this.toastMessage === msg) {
        this.toastMessage = '';
      }
    }, 4000);
  }
}
