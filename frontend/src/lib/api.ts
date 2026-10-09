import type { CreateProfileInput, ProfilesQuery, ProfilesResponse, Profile } from './types';
import { API_BASE_URL } from './constants';

/**
 * Typed error thrown by the API client.
 * `status` mirrors the HTTP status code (0 = network failure).
 */
export class ApiError extends Error {
  status: number;
  fieldErrors?: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors?: Record<string, string[]>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

function buildUrl(path: string, params?: Record<string, string | number | undefined>): string {
  const base = API_BASE_URL || '';
  const url = new URL(`${base}${path}`, window.location.origin);
  if (params) {
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== '') {
        url.searchParams.set(key, String(value));
      }
    }
  }
  // If API_BASE_URL is absolute, URL above may be wrong host — rebuild properly.
  if (base.startsWith('http')) {
    const absolute = new URL(`${path}`, base.endsWith('/') ? base : `${base}/`);
    if (params) {
      for (const [key, value] of Object.entries(params)) {
        if (value !== undefined && value !== '') {
          absolute.searchParams.set(key, String(value));
        }
      }
    }
    return absolute.toString();
  }
  // Same-origin fallback (useful for Vercel rewrites / Netlify proxies)
  return `${path}${url.search}`;
}

async function parseError(res: Response): Promise<never> {
  let message = `Request failed with status ${res.status}`;
  let fieldErrors: Record<string, string[]> | undefined;
  try {
    const data = await res.json();
    if (typeof data?.message === 'string' && data.message.length > 0) {
      message = data.message;
    } else if (typeof data?.error === 'string' && data.error.length > 0) {
      message = data.error;
    }
    if (data?.errors && typeof data.errors === 'object') {
      fieldErrors = data.errors as Record<string, string[]>;
    }
  } catch {
    // Non-JSON error body — keep default message.
  }
  throw new ApiError(res.status, message, fieldErrors);
}

export async function fetchProfiles(query: ProfilesQuery): Promise<ProfilesResponse> {
  const url = buildUrl('/api/profiles', {
    search: query.search?.trim() || undefined,
    track: query.track || undefined,
    page: query.page ?? 1,
    pageSize: query.pageSize ?? 12,
  });

  let res: Response;
  try {
    res = await fetch(url, { headers: { Accept: 'application/json' } });
  } catch {
    throw new ApiError(0, 'Network error. Check your connection and try again.');
  }
  if (!res.ok) await parseError(res);
  const data = (await res.json()) as ProfilesResponse;
  return {
    items: Array.isArray(data.items) ? data.items : [],
    total: typeof data.total === 'number' ? data.total : 0,
  };
}

export async function createProfile(input: CreateProfileInput): Promise<Profile> {
  const url = buildUrl('/api/profiles');

  let res: Response;
  try {
    res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({
        fullName: input.fullName.trim(),
        ...(input.githubUrl?.trim() ? { githubUrl: input.githubUrl.trim() } : {}),
        ...(input.linkedinUrl?.trim() ? { linkedinUrl: input.linkedinUrl.trim() } : {}),
        ...(input.track?.trim() ? { track: input.track.trim() } : {}),
      }),
    });
  } catch {
    throw new ApiError(0, 'Network error. Check your connection and try again.');
  }
  if (!res.ok) await parseError(res);
  return (await res.json()) as Profile;
}

/** Friendly message for list errors. */
export function toListErrorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 0) return err.message;
    return err.message || 'Something went wrong while loading students.';
  }
  return 'Something went wrong while loading students.';
}

/** Friendly message for create-form server errors. */
export function toCreateErrorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 409) return 'This profile already exists.';
    if (err.status === 429) return 'Too many attempts. Please wait a bit and try again.';
    if (err.status === 400) return err.message || 'Please check the highlighted fields.';
    if (err.status === 0) return err.message;
    return err.message || 'Something went wrong. Please try again.';
  }
  return 'Something went wrong. Please try again.';
}
