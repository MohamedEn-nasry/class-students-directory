import { Search, X } from 'lucide-react';
import { TRACKS } from '../lib/constants';
import { cx } from '../lib/utils';

interface Props {
  search: string;
  onSearchChange: (value: string) => void;
  track: string;
  onTrackChange: (value: string) => void;
  resultCount?: number;
}

export function SearchAndFilter({ search, onSearchChange, track, onTrackChange, resultCount }: Props) {
  return (
    <section aria-label="Search and filter students" className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-card dark:border-slate-800 dark:bg-slate-900 sm:p-5">
        <label htmlFor="student-search" className="text-sm font-semibold">
          Search by name
        </label>
        <div className="relative mt-2">
          <Search
            aria-hidden="true"
            className="pointer-events-none absolute left-3.5 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
          />
          <input
            id="student-search"
            type="search"
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder="e.g. Ada Lovelace"
            autoComplete="off"
            className="w-full rounded-xl border border-slate-200 bg-slate-50 py-2.5 pl-11 pr-10 text-sm placeholder:text-slate-400 dark:border-slate-700 dark:bg-slate-950 dark:placeholder:text-slate-500"
            aria-describedby={typeof resultCount === 'number' ? 'search-count' : undefined}
          />
          {search && (
            <button
              type="button"
              onClick={() => onSearchChange('')}
              aria-label="Clear search"
              className="absolute right-2.5 top-1/2 inline-flex h-7 w-7 -translate-y-1/2 items-center justify-center rounded-full text-slate-400 hover:bg-slate-200/70 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-200"
            >
              <X className="h-4 w-4" aria-hidden="true" />
            </button>
          )}
        </div>

        <div className="mt-4">
          <p id="track-filter-label" className="text-sm font-semibold">
            Filter by track
          </p>
          <div
            role="group"
            aria-labelledby="track-filter-label"
            className="mt-2 flex flex-wrap gap-2"
          >
            <FilterChip
              active={track === ''}
              onClick={() => onTrackChange('')}
              label="All tracks"
            />
            {TRACKS.map((t) => (
              <FilterChip
                key={t}
                active={track === t}
                onClick={() => onTrackChange(track === t ? '' : t)}
                label={t}
              />
            ))}
          </div>
        </div>

        {typeof resultCount === 'number' && (
          <p id="search-count" className="mt-3 text-xs text-slate-500 dark:text-slate-400" role="status">
            {resultCount} {resultCount === 1 ? 'result' : 'results'}
            {search.trim() ? ` for “${search.trim()}”` : ''}
            {track ? ` in ${track}` : ''}
          </p>
        )}
      </div>
    </section>
  );
}

function FilterChip({
  active,
  onClick,
  label,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={cx(
        'rounded-full border px-3.5 py-1.5 text-sm font-medium transition active:scale-[0.97]',
        active
          ? 'border-accent-600 bg-accent-600 text-white shadow-sm hover:bg-accent-700'
          : 'border-slate-200 bg-slate-50 text-slate-700 hover:border-accent-300 hover:bg-accent-50 hover:text-accent-700 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-300 dark:hover:border-accent-700 dark:hover:bg-accent-950 dark:hover:text-accent-200',
      )}
    >
      {label}
    </button>
  );
}
