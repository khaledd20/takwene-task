export interface Artist {
  id: string;
  name: string;
  email: string;
  country: string;
  createdAt: string;
}

export interface Dsp {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
}

export interface TrackDistribution {
  id: string;
  trackId: string;
  dspId: string;
  dspName: string;
  submittedAt: string;
  status: 'pending' | 'live' | 'rejected';
}

export interface Track {
  id: string;
  title: string;
  artistId: string;
  artistName: string;
  isrc: string;
  releaseDate: string;
  genre: string;
  status: 'draft' | 'submitted' | 'distributed';
  createdAt: string;
  distributions?: TrackDistribution[];
}

export interface CreateTrackRequest {
  title: string;
  artistId: string;
  isrc: string;
  releaseDate: string;
  genre: string;
}

export interface CreateArtistRequest {
  name: string;
  email: string;
  country: string;
}

export interface DistributeTrackRequest {
  dspIds: string[];
}

export interface UpdateTrackStatusRequest {
  status: 'draft' | 'submitted' | 'distributed';
}

export interface LoginResponse {
  token: string;
  email: string;
  role: string;
  expiresInSeconds: number;
}
