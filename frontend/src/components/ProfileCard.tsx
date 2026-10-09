import { Github, Linkedin } from 'lucide-react';
import type { Profile } from '../lib/types';
import { avatarColor, getInitials } from '../lib/utils';

export function ProfileCard({ profile }: { profile: Profile }) {
  const initials = getInitials(profile.fullName);
  const colors = avatarColor(profile.fullName);

  return (
    <article
      aria-label={`Profile of ${profile.fullName}`}
      className="flex flex-col rounded-2xl border border-slate-200 bg-white p-5 shadow-card transition hover:-translate-y-0.5 hover:shadow-lg dark:border-slate-800 dark:bg-slate-900"
    >
      <div className="flex items-start gap-3.5">
        <span
          aria-hidden="true"
          style={colors}
          className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full text-base font-bold"
        >
          {initials}
        </span>
        <div className="min-w-0 flex-1">
          <h3 className="truncate text-[15px] font-bold leading-tight">{profile.fullName}</h3>
          {profile.track ? (
            <span className="mt-1.5 inline-block rounded-full bg-accent-50 px-2.5 py-0.5 text-xs font-semibold text-accent-700 ring-1 ring-inset ring-accent-200 dark:bg-accent-950 dark:text-accent-200 dark:ring-accent-800">
              {profile.track}
            </span>
          ) : (
            <span className="mt-1.5 inline-block rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-500 ring-1 ring-inset ring-slate-200 dark:bg-slate-800 dark:text-slate-400 dark:ring-slate-700">
              No track listed
            </span>
          )}
        </div>
      </div>

      <div className="mt-4 flex items-center gap-2">
        {profile.githubUrl ? (
          <a
            href={profile.githubUrl}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={`Open ${profile.fullName}'s GitHub profile (opens in new tab)`}
            className="inline-flex h-10 flex-1 items-center justify-center gap-2 rounded-xl border border-slate-200 bg-slate-50 text-sm font-semibold text-slate-700 transition hover:border-slate-900 hover:bg-slate-900 hover:text-white dark:border-slate-700 dark:bg-slate-950 dark:text-slate-200 dark:hover:border-slate-100 dark:hover:bg-slate-100 dark:hover:text-slate-900"
          >
            <Github className="h-4 w-4" aria-hidden="true" />
            GitHub
          </a>
        ) : null}
        {profile.linkedinUrl ? (
          <a
            href={profile.linkedinUrl}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={`Open ${profile.fullName}'s LinkedIn profile (opens in new tab)`}
            className="inline-flex h-10 flex-1 items-center justify-center gap-2 rounded-xl border border-transparent bg-[#0A66C2] text-sm font-semibold text-white transition hover:bg-[#084e96]"
          >
            <Linkedin className="h-4 w-4" aria-hidden="true" />
            LinkedIn
          </a>
        ) : null}
        {!profile.githubUrl && !profile.linkedinUrl ? (
          <p className="text-sm text-slate-400">No links provided.</p>
        ) : null}
      </div>
    </article>
  );
}

export function ProfileCardSkeleton() {
  return (
    <div
      aria-hidden="true"
      className="rounded-2xl border border-slate-200 bg-white p-5 shadow-card dark:border-slate-800 dark:bg-slate-900"
    >
      <div className="flex items-start gap-3.5">
        <div className="csd-skeleton h-12 w-12 shrink-0 rounded-full" />
        <div className="flex-1 space-y-2 pt-1">
          <div className="csd-skeleton h-4 w-3/4 rounded" />
          <div className="csd-skeleton h-5 w-20 rounded-full" />
        </div>
      </div>
      <div className="mt-4 flex gap-2">
        <div className="csd-skeleton h-10 flex-1 rounded-xl" />
        <div className="csd-skeleton h-10 flex-1 rounded-xl" />
      </div>
    </div>
  );
}
