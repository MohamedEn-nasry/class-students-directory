import { useEffect, useRef } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Github, Linkedin, Loader2, User, X } from 'lucide-react';
import { useCreateProfile } from '../hooks/useProfiles';
import { TRACKS } from '../lib/constants';
import { ApiError, toCreateErrorMessage } from '../lib/api';
import { createProfileSchema, type CreateProfileFormValues } from '../lib/validation';
import { cx } from '../lib/utils';

interface Props {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  onError: (title: string, message: string) => void;
}

export function AddProfileModal({ open, onClose, onSuccess, onError }: Props) {
  const createMutation = useCreateProfile();
  const dialogRef = useRef<HTMLDivElement>(null);
  const firstFieldRef = useRef<HTMLInputElement | null>(null);
  const previouslyFocused = useRef<HTMLElement | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CreateProfileFormValues>({
    resolver: zodResolver(createProfileSchema),
    defaultValues: { fullName: '', githubUrl: '', linkedinUrl: '', track: '', website: '' },
    mode: 'onTouched',
  });
  const fullNameReg = register('fullName');

  // Focus management: remember trigger, focus first field, restore on close.
  useEffect(() => {
    if (open) {
      previouslyFocused.current = document.activeElement as HTMLElement | null;
      reset({ fullName: '', githubUrl: '', linkedinUrl: '', track: '', website: '' });
      createMutation.reset();
      const id = window.setTimeout(() => firstFieldRef.current?.focus(), 30);
      document.body.style.overflow = 'hidden';
      return () => {
        window.clearTimeout(id);
        document.body.style.overflow = '';
      };
    } else {
      document.body.style.overflow = '';
      previouslyFocused.current?.focus?.();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  // Escape to close + simple focus trap.
  useEffect(() => {
    if (!open) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.stopPropagation();
        onClose();
      } else if (e.key === 'Tab' && dialogRef.current) {
        const focusable = dialogRef.current.querySelectorAll<HTMLElement>(
          'button:not([disabled]), input, select, [tabindex]:not([tabindex="-1"])',
        );
        if (focusable.length === 0) return;
        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    };
    document.addEventListener('keydown', onKeyDown, true);
    return () => document.removeEventListener('keydown', onKeyDown, true);
  }, [open, onClose]);

  if (!open) return null;

  const onSubmit = handleSubmit(async (values) => {
    // Honeypot: silently "succeed" for bots without hitting the API.
    if (values.website && values.website.trim() !== '') {
      onSuccess();
      onClose();
      return;
    }
    try {
      await createMutation.mutateAsync({
        fullName: values.fullName.trim(),
        githubUrl: values.githubUrl || undefined,
        linkedinUrl: values.linkedinUrl || undefined,
        track: values.track || undefined,
      });
      onSuccess();
      onClose();
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.fieldErrors) {
        for (const [field, messages] of Object.entries(err.fieldErrors)) {
          if (field === 'fullName' || field === 'githubUrl' || field === 'linkedinUrl' || field === 'track') {
            setError(field, { type: 'server', message: messages[0] });
          }
        }
      }
      onError(
        err instanceof ApiError && err.status === 409
          ? 'This profile already exists'
          : err instanceof ApiError && err.status === 429
            ? 'Too many attempts'
            : 'Could not add profile',
        toCreateErrorMessage(err),
      );
    }
  });

  const busy = isSubmitting || createMutation.isPending;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center p-0 sm:items-center sm:p-6"
      role="presentation"
    >
      {/* Overlay */}
      <button
        type="button"
        aria-label="Close dialog"
        onClick={onClose}
        className="absolute inset-0 cursor-default bg-slate-950/60 backdrop-blur-[2px]"
      />
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="add-profile-title"
        aria-describedby="add-profile-desc"
        className="relative max-h-[92vh] w-full max-w-lg overflow-y-auto rounded-t-3xl bg-white p-6 shadow-2xl dark:bg-slate-900 sm:rounded-3xl sm:p-7"
      >
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 id="add-profile-title" className="text-xl font-extrabold tracking-tight">
              Add my profile
            </h2>
            <p id="add-profile-desc" className="mt-1 text-sm text-slate-500 dark:text-slate-400">
              Visible to everyone instantly. No account needed — just add at least one link.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close add profile dialog"
            className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-200"
          >
            <X className="h-5 w-5" aria-hidden="true" />
          </button>
        </div>

        <form onSubmit={onSubmit} noValidate className="mt-5 space-y-4">
          {/* Full name */}
          <div>
            <label htmlFor="ap-fullName" className="text-sm font-semibold">
              Full name <span aria-hidden="true" className="text-red-600">*</span>
            </label>
            <div className="relative mt-1.5">
              <User
                aria-hidden="true"
                className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
              />
              <input
                id="ap-fullName"
                type="text"
                autoComplete="name"
                placeholder="e.g. Ada Lovelace"
                aria-invalid={Boolean(errors.fullName)}
                aria-describedby={errors.fullName ? 'ap-fullName-error' : undefined}
                {...fullNameReg}
                ref={(e) => {
                  fullNameReg.ref(e);
                  firstFieldRef.current = e;
                }}
                className={cx(
                  'w-full rounded-xl border bg-slate-50 py-2.5 pl-10 pr-3 text-sm dark:bg-slate-950',
                  errors.fullName
                    ? 'border-red-500'
                    : 'border-slate-200 dark:border-slate-700',
                )}
              />
            </div>
            {errors.fullName && (
              <p id="ap-fullName-error" role="alert" className="mt-1.5 text-sm text-red-600 dark:text-red-400">
                {errors.fullName.message}
              </p>
            )}
          </div>

          {/* GitHub */}
          <div>
            <label htmlFor="ap-github" className="text-sm font-semibold">
              GitHub URL
            </label>
            <div className="relative mt-1.5">
              <Github
                aria-hidden="true"
                className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
              />
              <input
                id="ap-github"
                type="url"
                inputMode="url"
                autoComplete="url"
                placeholder="https://github.com/your-handle"
                aria-invalid={Boolean(errors.githubUrl)}
                aria-describedby={errors.githubUrl ? 'ap-github-error' : 'ap-github-hint'}
                {...register('githubUrl')}
                className={cx(
                  'w-full rounded-xl border bg-slate-50 py-2.5 pl-10 pr-3 text-sm dark:bg-slate-950',
                  errors.githubUrl
                    ? 'border-red-500'
                    : 'border-slate-200 dark:border-slate-700',
                )}
              />
            </div>
            {errors.githubUrl ? (
              <p id="ap-github-error" role="alert" className="mt-1.5 text-sm text-red-600 dark:text-red-400">
                {errors.githubUrl.message}
              </p>
            ) : (
              <p id="ap-github-hint" className="mt-1.5 text-xs text-slate-500 dark:text-slate-400">
                Must look like https://github.com/username
              </p>
            )}
          </div>

          {/* LinkedIn */}
          <div>
            <label htmlFor="ap-linkedin" className="text-sm font-semibold">
              LinkedIn URL
            </label>
            <div className="relative mt-1.5">
              <Linkedin
                aria-hidden="true"
                className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
              />
              <input
                id="ap-linkedin"
                type="url"
                inputMode="url"
                autoComplete="url"
                placeholder="https://www.linkedin.com/in/your-handle"
                aria-invalid={Boolean(errors.linkedinUrl)}
                aria-describedby={errors.linkedinUrl ? 'ap-linkedin-error' : 'ap-linkedin-hint'}
                {...register('linkedinUrl')}
                className={cx(
                  'w-full rounded-xl border bg-slate-50 py-2.5 pl-10 pr-3 text-sm dark:bg-slate-950',
                  errors.linkedinUrl
                    ? 'border-red-500'
                    : 'border-slate-200 dark:border-slate-700',
                )}
              />
            </div>
            {errors.linkedinUrl ? (
              <p id="ap-linkedin-error" role="alert" className="mt-1.5 text-sm text-red-600 dark:text-red-400">
                {errors.linkedinUrl.message}
              </p>
            ) : (
              <p id="ap-linkedin-hint" className="mt-1.5 text-xs text-slate-500 dark:text-slate-400">
                Must look like https://www.linkedin.com/in/username
              </p>
            )}
          </div>

          {/* Track */}
          <div>
            <label htmlFor="ap-track" className="text-sm font-semibold">
              Track <span className="font-normal text-slate-400">(optional)</span>
            </label>
            <select
              id="ap-track"
              aria-invalid={Boolean(errors.track)}
              aria-describedby={errors.track ? 'ap-track-error' : undefined}
              {...register('track')}
              className={cx(
                'mt-1.5 w-full rounded-xl border bg-slate-50 px-3 py-2.5 text-sm dark:bg-slate-950',
                errors.track ? 'border-red-500' : 'border-slate-200 dark:border-slate-700',
              )}
            >
              <option value="">Select a track…</option>
              {TRACKS.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
            {errors.track && (
              <p id="ap-track-error" role="alert" className="mt-1.5 text-sm text-red-600 dark:text-red-400">
                {errors.track.message}
              </p>
            )}
          </div>

          {/* Honeypot — hidden from sighted + keyboard users, but bots fill it */}
          <div aria-hidden="true" className="absolute -left-[9999px] h-px w-px overflow-hidden opacity-0">
            <label htmlFor="ap-website">Website (leave blank)</label>
            <input
              id="ap-website"
              type="text"
              tabIndex={-1}
              autoComplete="off"
              {...register('website')}
            />
          </div>

          <div className="flex flex-col-reverse gap-2 pt-1 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={busy}
              className="rounded-full border border-slate-300 px-5 py-2.5 text-sm font-semibold hover:bg-slate-100 disabled:opacity-50 dark:border-slate-600 dark:hover:bg-slate-800"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={busy}
              className="inline-flex items-center justify-center gap-2 rounded-full bg-accent-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-accent-700 disabled:opacity-60"
            >
              {busy && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
              {busy ? 'Adding…' : 'Add my profile'}
            </button>
          </div>
          <p className="text-center text-xs text-slate-400 sm:text-left">
            At least one link (GitHub or LinkedIn) is required.
          </p>
        </form>
      </div>
    </div>
  );
}
