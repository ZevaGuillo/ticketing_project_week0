"use client"

import useSWR from "swr"
import { getUserOpportunities, type UserOpportunity } from "@/lib/api/waitlist"

export function useOpportunities(eventId: string, userId: string) {
  return useSWR<UserOpportunity[]>(
    eventId && userId ? `opportunities-${eventId}-${userId}` : null,
    () => getUserOpportunities(eventId, userId),
    {
      revalidateOnFocus: true,
      revalidateIfStale: false,
      dedupingInterval: 5000,
      refreshInterval: 10000,
    }
  )
}