export const TRACKS = [
  'Frontend',
  'Backend',
  'Fullstack',
  'Data / AI',
  'Mobile',
  'DevOps',
  'UI/UX',
] as const;

export type Track = (typeof TRACKS)[number];

export const PAGE_SIZE = 12;

export const API_BASE_URL =
  (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? '';
