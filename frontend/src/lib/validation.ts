import { z } from 'zod';
import { TRACKS } from './constants';

const GITHUB_RE = /^https:\/\/(www\.)?github\.com\/[A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?(\/[A-Za-z0-9_.-]+)*\/?(\?.*)?(#.*)?$/;
const LINKEDIN_RE = /^https:\/\/(www\.)?linkedin\.com\/(in|company)\/[A-Za-z0-9-_%]+\/?(\?.*)?(#.*)?$/;

const optionalUrl = (pattern: RegExp, label: string) =>
  z
    .string()
    .trim()
    .max(300, `${label} is too long.`)
    .optional()
    .or(z.literal(''))
    .refine((v) => !v || pattern.test(v), {
      message: `Enter a valid ${label} (https://…).`,
    })
    .transform((v) => (v === '' ? undefined : v));

export const createProfileSchema = z
  .object({
    fullName: z
      .string()
      .trim()
      .min(2, 'Enter your full name (at least 2 characters).')
      .max(80, 'Name must be 80 characters or fewer.'),
    githubUrl: optionalUrl(GITHUB_RE, 'GitHub URL'),
    linkedinUrl: optionalUrl(LINKEDIN_RE, 'LinkedIn URL'),
    track: z
      .string()
      .trim()
      .max(40)
      .optional()
      .or(z.literal(''))
      .transform((v) => (v === '' ? undefined : v))
      .refine((v) => !v || (TRACKS as readonly string[]).includes(v), {
        message: 'Pick a track from the list.',
      }),
    // Honeypot: real users leave it empty; bots fill it.
    website: z.string().max(200).optional().default(''),
  })
  .superRefine((data, ctx) => {
    if (!data.githubUrl && !data.linkedinUrl) {
      const message = 'Add at least one link (GitHub or LinkedIn).';
      ctx.addIssue({ code: z.ZodIssueCode.custom, message, path: ['githubUrl'] });
      ctx.addIssue({ code: z.ZodIssueCode.custom, message, path: ['linkedinUrl'] });
    }
  });

export type CreateProfileFormValues = z.infer<typeof createProfileSchema>;
