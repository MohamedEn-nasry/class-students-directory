-- Reference DDL for the pre-existing table public.profiles.
-- DO NOT run CREATE TABLE if it already exists; this file documents the
-- constraints the API mirrors in C# (Services/ProfileUrls.cs).
--
-- NOTE on regex dialects: PostgreSQL POSIX (~) does not support PCRE
-- non-capturing groups (?:...), so the DB uses (...) where C# uses (?:...).
-- The languages are semantically identical — same accepted URLs.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS public.profiles (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  full_name varchar(60) NOT NULL CHECK (char_length(btrim(full_name)) BETWEEN 1 AND 60),
  github_url varchar(200) NULL UNIQUE,
  linkedin_url varchar(300) NULL UNIQUE,
  track varchar(50) NULL CHECK (track IS NULL OR char_length(btrim(track)) BETWEEN 1 AND 50),
  is_approved boolean NOT NULL DEFAULT true,
  created_at timestamptz NOT NULL DEFAULT now(),
  ip_hash varchar(64) NULL,

  -- At least one link required (mirrored in ProfileValidator).
  CONSTRAINT ck_profiles_at_least_one_link
    CHECK (github_url IS NOT NULL OR linkedin_url IS NOT NULL),

  -- URL formats, mirrored in ProfileUrls.GithubPattern / LinkedinPattern.
  CONSTRAINT ck_profiles_github_url_format CHECK (
    github_url IS NULL OR
    github_url ~ '^https://github\.com/[A-Za-z0-9]([A-Za-z0-9-]{0,37}[A-Za-z0-9])?(/[A-Za-z0-9_.\-]+)*/?$'
  ),
  CONSTRAINT ck_profiles_linkedin_url_format CHECK (
    linkedin_url IS NULL OR
    linkedin_url ~ '^https://([a-z0-9-]+\.)?linkedin\.com/(in|company)/[A-Za-z0-9_.\-%]+(/[A-Za-z0-9_.\-%]*)*/?$'
  )
);

CREATE INDEX IF NOT EXISTS ix_profiles_created_at ON public.profiles (created_at DESC);
CREATE INDEX IF NOT EXISTS ix_profiles_track ON public.profiles (track) WHERE track IS NOT NULL;

-- If the table already exists without the CHECKs, add them idempotently:
-- ALTER TABLE public.profiles ADD CONSTRAINT ... (same definitions as above).
