import { GraduationCap, Plus } from 'lucide-react';
import { ThemeToggle } from './ThemeToggle';

interface Props {
  theme: 'light' | 'dark';
  onToggleTheme: () => void;
  onAdd: () => void;
}

export function SiteHeader({ theme, onToggleTheme, onAdd }: Props) {
  return (
    <header className="sticky top-0 z-30 border-b border-slate-200/70 bg-white/80 backdrop-blur dark:border-slate-800 dark:bg-slate-950/80">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-3 px-4 sm:px-6 lg:px-8">
        <a href="#top" className="flex items-center gap-2.5 rounded-lg">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-accent-600 text-white shadow-sm">
            <GraduationCap className="h-5 w-5" aria-hidden="true" />
          </span>
          <span className="text-sm font-bold tracking-tight sm:text-base">
            Class Students Directory
          </span>
        </a>
        <div className="flex items-center gap-2">
          <ThemeToggle theme={theme} onToggle={onToggleTheme} />
          <button
            type="button"
            onClick={onAdd}
            className="hidden items-center gap-1.5 rounded-full bg-accent-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-accent-700 sm:inline-flex"
          >
            <Plus className="h-4 w-4" aria-hidden="true" />
            Add my profile
          </button>
        </div>
      </div>
    </header>
  );
}
