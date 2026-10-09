# Class Students Directory

Public, no-login directory of students' GitHub / LinkedIn links. Anyone can browse, anyone can add their own profile.

**Stack:** React 18 + Vite 5 + TypeScript + Tailwind CSS 3, TanStack Query 5, React Hook Form + Zod, lucide-react. No `dangerouslySetInnerHTML` anywhere.

## Features

- Hero with title, subtitle, live student counter (`total` from `GET /api/profiles`), "Add my profile" button
- Debounced (400 ms) name search + track filter chips (`All` + Frontend, Backend, Fullstack, Data/AI, Mobile, DevOps, UI/UX)
- Cards grid 1 / 2 / 3 / 4 cols: initials avatar, name, track badge, GitHub + LinkedIn buttons (`target="_blank" rel="noopener noreferrer"`)
- Skeleton loading, empty state, error + retry, "Load more" (infinite `page`/`pageSize`)
- Add-profile modal: `fullName`, GitHub URL, LinkedIn URL, `track`; ≥1 link required; client-side Zod URL-pattern validation; inline errors; loading state; success toast; `409 → "This profile already exists"`, `429 → "Too many attempts"`; hidden honeypot input
- Light/dark mode persisted to `localStorage` (`csd-theme`), no flash (inline script in `index.html`)
- Mobile-first, sticky "Add" FAB on mobile, skip link, focus trap + Escape in modal, labelled controls, WCAG AA contrast + visible focus rings

## API contract

Base URL from `VITE_API_URL` (see `.env.example`).

- `GET /api/profiles?search=&track=&page=&pageSize=` → `{ items: Profile[], total: number }`
- `POST /api/profiles { fullName, githubUrl?, linkedinUrl?, track? }` → `201 | 400 | 409 | 429`
- `Profile = { id, fullName, githubUrl?, linkedinUrl?, track?, createdAt }`

Typed client lives in `src/lib/api.ts` (`ApiError.status`, `fetchProfiles`, `createProfile`).

## File map

```
index.html
vite.config.ts  tsconfig{,.node}.json  tailwind.config.js  postcss.config.js
vercel.json  netlify.toml  .env.example
src/
  main.tsx  App.tsx  index.css  vite-env.d.ts
  lib/ types.ts  constants.ts  api.ts  validation.ts  utils.ts
  hooks/ useProfiles.ts  useDebouncedValue.ts  useTheme.ts
  components/ SiteHeader.tsx  ThemeToggle.tsx  Hero.tsx  SearchAndFilter.tsx
              ProfileCard.tsx  States.tsx  AddProfileModal.tsx  Toast.tsx  SiteFooter.tsx
```

## Run locally

```bash
npm install
cp .env.example .env   # set VITE_API_URL to your backend
npm run dev            # http://localhost:5173
npm run build          # typecheck + production build -> dist/
npm run preview
```

## Deploy

### Vercel

1. Push this folder to GitHub.
2. Vercel dashboard → **Add New → Project → Import** the repo.
   - Framework preset: **Vite**. Build command: `npm run build`. Output: `dist`.
3. **Environment Variables** → add `VITE_API_URL=https://your-backend-host` (production).
4. Optional same-origin API proxy (avoids CORS): edit `vercel.json` — replace `YOUR_BACKEND_HOST` with your API host:
   ```json
   { "rewrites": [{ "source": "/api/:path*", "destination": "https://YOUR_BACKEND_HOST/api/:path*" }] }
   ```
   Then you may leave `VITE_API_URL` empty.
5. **Deploy**. Every push to `main` redeploys.

CLI alternative: `npx vercel --prod`.

### Netlify

1. Push to GitHub → Netlify **Add new site → Import an existing project**.
   - Build command: `npm run build`. Publish directory: `dist`.
2. **Site settings → Environment variables** → `VITE_API_URL=https://your-backend-host`.
3. Optional API proxy is pre-wired in `netlify.toml` — replace `YOUR_BACKEND_HOST` with your API host so `/api/*` forwards with status 200 (avoids CORS). SPA fallback `/ → /index.html` is also included.
4. **Deploy site**. Enable **Auto publish** for continuous deploys.

### Any static host

`npm run build` → upload `dist/` anywhere. Set `VITE_API_URL` at build time.
