import { Plus, Users } from 'lucide-react';

interface Props {
  total: number | undefined;
  isLoadingTotal: boolean;
  onAdd: () => void;
}

export function Hero({ total, isLoadingTotal, onAdd }: Props) {
  return (
    <section aria-labelledby="hero-title" className="relative overflow-hidden">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 bg-gradient-to-br from-accent-50 via-white to-white dark:from-accent-950/60 dark:via-slate-950 dark:to-slate-950"
      />
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-24 right-0 h-72 w-72 rounded-full bg-accent-200/40 blur-3xl dark:bg-accent-600/20"
      />
      <div className="relative mx-auto max-w-7xl px-4 pb-8 pt-10 sm:px-6 sm:pt-14 lg:px-8">
        <p className="inline-flex items-center gap-1.5 rounded-full border border-accent-200 bg-accent-50 px-3 py-1 text-xs font-semibold text-accent-700 dark:border-accent-800 dark:bg-accent-950 dark:text-accent-200">
          <Users className="h-3.5 w-3.5" aria-hidden="true" />
          Open to every student · No login needed
        </p>
        <h1
          id="hero-title"
          className="mt-4 max-w-2xl text-3xl font-extrabold tracking-tight sm:text-4xl lg:text-5xl"
        >
          Class Students Directory
        </h1>
        <p className="mt-3 max-w-2xl text-base text-slate-600 dark:text-slate-300 sm:text-lg">
          Browse every classmate&apos;s GitHub and LinkedIn in one place — and add your own
          profile in seconds so others can find you.
        </p>
        <div className="mt-6 flex flex-wrap items-center gap-3">
          <button
            type="button"
            onClick={onAdd}
            className="inline-flex items-center gap-2 rounded-full bg-accent-600 px-5 py-2.5 text-sm font-semibold text-white shadow-md transition hover:bg-accent-700 active:scale-[0.98]"
          >
            <Plus className="h-4 w-4" aria-hidden="true" />
            Add my profile
          </button>
          <p
            className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            role="status"
            aria-live="polite"
            aria-label={typeof total === 'number' ? `${total} students listed` : 'Loading student count'}
          >
            <span
              aria-hidden="true"
              className="relative flex h-2.5 w-2.5 rounded-full bg-emerald-500"
            />
            {isLoadingTotal ? (
              <span className="inline-block h-4 w-24 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
            ) : (
              <span>
                <strong className="font-bold tabular-nums">{total ?? 0}</strong>{' '}
                {(total ?? 0) === 1 ? 'student' : 'students'} listed
              </span>
            )}
          </p>
        </div>
      </div>
    </section>
  );
}
