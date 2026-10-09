export function SiteFooter() {
  return (
    <footer className="mx-auto max-w-7xl px-4 pb-24 pt-10 sm:px-6 sm:pb-10 lg:px-8">
      <div className="flex flex-col items-center justify-between gap-3 border-t border-slate-200 pt-6 text-center text-xs text-slate-500 dark:border-slate-800 dark:text-slate-400 sm:flex-row sm:text-left">
        <p>
          <strong className="font-semibold">Class Students Directory</strong> — public, community-run.
          Anyone can browse, anyone can add.
        </p>
        <p>
          Links open in a new tab. Only share profiles you own or have permission to share.
        </p>
      </div>
    </footer>
  );
}
