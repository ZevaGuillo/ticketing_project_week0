import { API_CONFIG } from "./config"

const WAITLIST_API = `${API_CONFIG.gateway}/api/waitlist`

export interface JoinWaitlistRequest {
  eventId: string
  section: string
}

export interface WaitlistStatusResponse {
  waitlistEntryId: string
  userId: string
  eventId: string
  section: string
  status: "ACTIVE" | "NOTIFIED" | "EXPIRED" | "CONVERTED" | "CANCELLED"
  queuePosition: number
  joinedAt: string
  notifiedAt?: string
  cancelledAt?: string
}

export async function joinWaitlist(request: JoinWaitlistRequest, userId: string): Promise<WaitlistStatusResponse> {
  const res = await fetch(`${WAITLIST_API}/join`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-User-Id": userId,
      "Cache-Control": "no-cache",
    },
    body: JSON.stringify(request),
    cache: "no-store",
  })

  if (!res.ok) {
    const error = await res.json().catch(() => ({}))
    throw new Error(error.error || error.message || "Failed to join waitlist")
  }

  return res.json()
}

export async function getWaitlistStatus(
  eventId: string,
  section: string,
  userId: string
): Promise<WaitlistStatusResponse | null> {
  const params = new URLSearchParams({ eventId, section })
  const res = await fetch(`${WAITLIST_API}/status?${params}`, {
    method: "GET",
    headers: {
      "X-User-Id": userId,
      "Cache-Control": "no-cache",
    },
    cache: "no-store",
  })

  if (res.status === 404) {
    return null
  }

  if (!res.ok) {
    const error = await res.json().catch(() => ({}))
    throw new Error(error.error || "Failed to get waitlist status")
  }

  return res.json()
}

export async function getAllWaitlistStatus(
  eventId: string,
  userId: string
): Promise<WaitlistStatusResponse[]> {
  const sections = ["General", "VIP", "A", "B", "C", "D"]
  const results: WaitlistStatusResponse[] = []
  
  for (const section of sections) {
    const status = await getWaitlistStatus(eventId, section, userId)
    if (status && status.status !== "CANCELLED") {
      results.push(status)
    }
  }
  
  return results
}

export async function cancelWaitlist(
  eventId: string,
  section: string,
  userId: string
): Promise<void> {
  const params = new URLSearchParams({ eventId, section })
  const res = await fetch(`${WAITLIST_API}/cancel?${params}`, {
    method: "DELETE",
    headers: {
      "X-User-Id": userId,
    },
  })

  if (!res.ok && res.status !== 404) {
    const error = await res.json().catch(() => ({}))
    throw new Error(error.error || "Failed to cancel waitlist")
  }
}

export interface UserOpportunity {
  opportunityId: string
  seatId: string
  section: string
  token: string
  status: string
  expiresAt: string
}

export async function getUserOpportunities(
  eventId: string,
  userId: string
): Promise<UserOpportunity[]> {
  const params = new URLSearchParams({ eventId })
  const res = await fetch(`${WAITLIST_API}/my-opportunities?${params}`, {
    method: "GET",
    headers: {
      "X-User-Id": userId,
      "Cache-Control": "no-cache",
    },
    cache: "no-store",
  })

  if (!res.ok) {
    const error = await res.json().catch(() => ({}))
    throw new Error(error.error || "Failed to get user opportunities")
  }

  return res.json()
}