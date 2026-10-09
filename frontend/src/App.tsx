import { useMemo, useState } from 'react';
import { Plus } from 'lucide-react';
import { SiteHeader } from './components/SiteHeader';
import { Hero } from './components/Hero';
import { SearchAndFilter } from './components/SearchAndFilter';
import { ProfileCard, ProfileCardSkeleton } from './components/ProfileCard';
import { EmptyState, ErrorState } from './components/States';
import { AddProfileModal } from './components/AddProfileModal';
import { ToastStack, type ToastItem } from './components/Toast';
import { SiteFooter } from './components/SiteFooter';
import { useDebouncedValue } from './hooks/useDebouncedValue';
import { useProfiles } from './hooks/useProfiles';
import { useTheme } from './hooks/useTheme';
import { toListErrorMessage } from './lib/api';

let toastId = 0;

export default function App() {
  const { theme, toggle } = useTheme();
  const [modalOpen, setModalOpen] = useState(false);
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const [search, setSearch] = useState('');
  const [track, setTrack] = useState('');
  const debouncedSearch = useDebouncedValue(search, 400);

  const filter = useMemo(
    () => ({ search: debouncedSearch, track }),
    [debouncedSearch, track],
  );
  const profilesQuery = useProfiles(filter);

  const pages = profilesQuery.data?.pages ?? [];
  const items = pages.flatMap((p) => p.items);
  const total = pages[0]?.total;

  const pushToast = (kind: ToastItem['kind'], title: string, message?: string) => {
    toastId += 1;
    const id = toastId;
    setToasts((prev) => [...prev.slice(-2), { id, kind, title, message }]);
  };
  const dismissToast = (id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  const openModal = () => setModalOpen(true);
  const resetFilters = () => {
    setSearch('');
    setTrack('');
  };

  const isInitialLoading = profilesQuery.isLoading;
  const isEmpty = !isInitialLoading && !profilesQuery.isError && items.length === 0;

  return (
    <div id="top" className="flex min-h-screen flex-col">
      <a
        href="#students-heading"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-[80] focus:rounded-full focus:bg-accent-600 focus:px-4 focus:py-2 focus:text-sm focus:font-semibold focus:text-white"
      >
        Skip to students
      </a>

      <SiteHeader theme={theme} onToggleTheme={toggle} onAdd={openModal} />

      <main className="flex-1">
        <Hero
          total={total}
          isLoadingTotal={profilesQuery.isLoading}
          onAdd={openModal}
        />

        <div className="mt-6">
          <SearchAndFilter
            search={search}
            onSearchChange={setSearch}
            track={track}
            onTrackChange={setTrack}
            resultCount={typeof total === 'number' && !profilesQuery.isError ? total : undefined}
          />
        </div>

        <section
          aria-labelledby="students-heading"
          className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8"
        >
          <h2 id="students-heading" className="sr-only">
            Students
          </h2>

          {profilesQuery.isError ? (
            <div className="grid grid-cols-1 gap-4">
              <ErrorState
                message={toListErrorMessage(profilesQuery.error)}
                onRetry={() => profilesQuery.refetch()}
              />
            </div>
          ) : (
            <>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                {isInitialLoading
                  ? Array.from({ length: 8 }).map((_, i) => <ProfileCardSkeleton key={i} />)
                  : items.map((profile) => <ProfileCard key={profile.id} profile={profile} />)}

                {isEmpty && <EmptyState onReset={resetFilters} onAdd={openModal} />}

                {/* Appending next page: show skeletons while keeping existing cards */}
                {profilesQuery.isFetchingNextPage &&
                  Array.from({ length: 4 }).map((_, i) => (
                    <ProfileCardSkeleton key={`next-${i}`} />
                  ))}
              </div>

              {/* Background refetch indicator (search/track change) */}
              {profilesQuery.isFetching && !isInitialLoading && !profilesQuery.isFetchingNextPage && (
                <p className="mt-4 text-center text-xs text-slate-400" role="status">
                  Updating results…
                </p>
              )}

              <div className="mt-8 flex justify-center">
                {profilesQuery.hasNextPage ? (
                  <button
                    type="button"
                    onClick={() => profilesQuery.fetchNextPage()}
                    disabled={profilesQuery.isFetchingNextPage}
                    className="rounded-full border border-slate-300 bg-white px-6 py-2.5 text-sm font-semibold shadow-sm transition hover:bg-slate-100 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:hover:bg-slate-800"
                  >
                    {profilesQuery.isFetchingNextPage ? 'Loading…' : 'Load more'}
                  </button>
                ) : (
                  !isInitialLoading &&
                  items.length > 0 && (
                    <p className="text-sm text-slate-400">
                      You&apos;ve seen all {total ?? items.length}{' '}
                      {(total ?? items.length) === 1 ? 'student' : 'students'}.
                    </p>
                  )
                )}
              </div>
            </>
          )}
        </section>
      </main>

      <SiteFooter />

      {/* Sticky "Add" button on mobile */}
      <button
        type="button"
        onClick={openModal}
        className="fixed bottom-4 right-4 z-40 inline-flex items-center gap-2 rounded-full bg-accent-600 px-5 py-3 text-sm font-semibold text-white shadow-xl transition hover:bg-accent-700 active:scale-95 sm:hidden"
        aria-label="Add my profile"
      >
        <Plus className="h-5 w-5" aria-hidden="true" />
        Add
      </button>

      <AddProfileModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onSuccess={() =>
          pushToast('success', 'Profile added!', 'Your profile is now visible in the directory.')
        }
        onError={(title, message) => pushToast('error', title, message)}
      />

      <ToastStack toasts={toasts} onDismiss={dismissToast} />
    </div>
  );
}
