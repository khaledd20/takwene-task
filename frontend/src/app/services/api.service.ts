import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  Artist,
  Dsp,
  Track,
  CreateTrackRequest,
  CreateArtistRequest,
  DistributeTrackRequest,
  UpdateTrackStatusRequest,
  LoginResponse
} from '../models/takwene.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = 'http://localhost:5000/api';
  private readonly tokenKey = 'takwene_token';
  private readonly userKey = 'takwene_user';

  // Reactive state signals
  readonly currentUser = signal<string | null>(this.getStoredUser());
  readonly isAuthenticated = signal<boolean>(!!this.getStoredToken());

  constructor(private http: HttpClient) {}

  private getStoredToken(): string | null {
    if (typeof window !== 'undefined' && window.localStorage) {
      return localStorage.getItem(this.tokenKey);
    }
    return null;
  }

  private getStoredUser(): string | null {
    if (typeof window !== 'undefined' && window.localStorage) {
      return localStorage.getItem(this.userKey);
    }
    return null;
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.getStoredToken();
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  }

  // Auth methods
  login(email: string = 'admin@takwene.com', password: string = 'Password123!'): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/auth/login`, { email, password }).pipe(
      tap((res) => {
        if (typeof window !== 'undefined' && window.localStorage) {
          localStorage.setItem(this.tokenKey, res.token);
          localStorage.setItem(this.userKey, res.email);
        }
        this.currentUser.set(res.email);
        this.isAuthenticated.set(true);
      })
    );
  }

  logout(): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.removeItem(this.tokenKey);
      localStorage.removeItem(this.userKey);
    }
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
  }

  // Artists
  getArtists(): Observable<Artist[]> {
    return this.http.get<Artist[]>(`${this.baseUrl}/artists`);
  }

  createArtist(request: CreateArtistRequest): Observable<Artist> {
    return this.http.post<Artist>(`${this.baseUrl}/artists`, request, {
      headers: this.getAuthHeaders()
    });
  }

  // DSPs
  getDsps(): Observable<Dsp[]> {
    return this.http.get<Dsp[]>(`${this.baseUrl}/dsps`);
  }

  // Tracks
  getTracks(status?: string, genre?: string, artistId?: string): Observable<Track[]> {
    let params = new HttpParams();
    if (status && status !== 'all') {
      params = params.set('status', status.toLowerCase());
    }
    if (genre) {
      params = params.set('genre', genre);
    }
    if (artistId) {
      params = params.set('artistId', artistId);
    }

    return this.http.get<Track[]>(`${this.baseUrl}/tracks`, { params });
  }

  getTrackById(id: string): Observable<Track> {
    return this.http.get<Track>(`${this.baseUrl}/tracks/${id}`);
  }

  createTrack(request: CreateTrackRequest): Observable<Track> {
    return this.http.post<Track>(`${this.baseUrl}/tracks`, request, {
      headers: this.getAuthHeaders()
    });
  }

  distributeTrack(id: string, request: DistributeTrackRequest): Observable<Track> {
    return this.http.post<Track>(`${this.baseUrl}/tracks/${id}/distribute`, request, {
      headers: this.getAuthHeaders()
    });
  }

  updateTrackStatus(id: string, request: UpdateTrackStatusRequest): Observable<Track> {
    return this.http.patch<Track>(`${this.baseUrl}/tracks/${id}/status`, request, {
      headers: this.getAuthHeaders()
    });
  }
}
