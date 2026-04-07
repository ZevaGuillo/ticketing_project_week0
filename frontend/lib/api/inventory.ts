import { API_CONFIG, authHeaders } from "./config"
import type {
  CreateReservationRequest,
  CreateReservationResponse,
} from "@/lib/types"

export async function createReservation(
  data: CreateReservationRequest,
  userId: string
): Promise<CreateReservationResponse> {
  // If user has an opportunity token, use the waitlist opportunity endpoint
  // which validates the token and creates the reservation in one call
  if (data.opportunityToken) {
    const res = await fetch(`${API_CONFIG.gateway}/api/waitlist/opportunity/${data.opportunityToken}`, {
      method: "GET",
      headers: authHeaders({ 
        "X-User-Id": userId,
      }),
    })
    
    if (res.status === 404) {
      throw new Error("Opportunity not found or expired")
    }
    if (res.status === 401) {
      throw new Error("Unauthorized - Please login")
    }
    if (!res.ok) {
      const error = await res.json().catch(() => ({}))
      throw new Error(error.error || "Failed to validate opportunity")
    }
    
    // Transform ValidateOpportunityResult to CreateReservationResponse format
    const opportunityResult = await res.json()
    return {
      reservationId: opportunityResult.reservationId,
      seatId: opportunityResult.seatId,
      customerId: opportunityResult.userId,
      expiresAt: opportunityResult.expiresAt,
      status: "active"
    }
  }
  
  // Regular reservation flow for users without opportunities
  const res = await fetch(`${API_CONFIG.gateway}${API_CONFIG.inventory}/reservations`, {
    method: "POST",
    headers: authHeaders({ 
      "Content-Type": "application/json",
      "X-User-Id": userId,
    }),
    body: JSON.stringify({
      seatId: data.seatId,
      eventId: data.eventId,
      customerId: data.customerId
    }),
  })
  
  if (res.status === 401) {
    throw new Error("Unauthorized - Please login")
  }
  if (res.status === 400) {
    const error = await res.json().catch(() => ({}))
    throw new Error(error.detail || "Bad request")
  }
  if (res.status === 404) {
    throw new Error("Seat not found")
  }
  if (res.status === 409) {
    throw new Error("Seat is already reserved")
  }
  if (!res.ok) {
    throw new Error(
      `Failed to create reservation: ${res.status} ${res.statusText}`
    )
  }
  return res.json()
}
