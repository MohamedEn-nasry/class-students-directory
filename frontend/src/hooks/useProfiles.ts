import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { createProfile, fetchProfiles } from '../lib/api';
import { PAGE_SIZE } from '../lib/constants';
import type { CreateProfileInput } from '../lib/types';

export interface ProfilesFilter {
  search: string;
  track: string; // '' = all
}

export function useProfiles(filter: ProfilesFilter) {
  return useInfiniteQuery({
    queryKey: ['profiles', filter.search.trim(), filter.track],
    queryFn: ({ pageParam = 1 }) =>
      fetchProfiles({
        search: filter.search.trim() || undefined,
        track: filter.track || undefined,
        page: pageParam as number,
        pageSize: PAGE_SIZE,
      }),
    initialPageParam: 1,
    getNextPageParam: (lastPage, allPages) => {
      const loaded = allPages.reduce((n, p) => n + p.items.length, 0);
      return loaded < lastPage.total ? allPages.length + 1 : undefined;
    },
    staleTime: 30_000,
  });
}

export function useCreateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateProfileInput) => createProfile(input),
    onSuccess: () => {
      // Refresh every profiles list so the new card appears.
      void queryClient.invalidateQueries({ queryKey: ['profiles'] });
    },
  });
}
