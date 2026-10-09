export interface Profile {
  id: string;
  fullName: string;
  githubUrl?: string | null;
  linkedinUrl?: string | null;
  track?: string | null;
  createdAt: string;
}

export interface ProfilesResponse {
  items: Profile[];
  total: number;
}

export interface ProfilesQuery {
  search?: string;
  track?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateProfileInput {
  fullName: string;
  githubUrl?: string;
  linkedinUrl?: string;
  track?: string;
}
