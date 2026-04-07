export const API_CONFIG = {
  gateway: process.env.NEXT_PUBLIC_GATEWAY_URL || "http://localhost:5000",
  auth: {
    login: "/auth/token",
    register: "/auth/register",
  },
  catalog: "/catalog",
  inventory: "/inventory",
  ordering: "/ordering",
  payment: "/payment",
} as const

export const RETRY_DELAY_MS = 3000
export const RETRY_ADD_TO_CART_ATTEMPTS = 3
export const RESERVATION_MINUTES = 15

export function getAuthToken(): string | null {
  if (typeof window === "undefined") return null
  return localStorage.getItem("auth-token")
}

export function authHeaders(extra: Record<string, string> = {}): Record<string, string> {
  const token = getAuthToken()
  const headers: Record<string, string> = { ...extra }
  if (token) {
    headers["Authorization"] = `Bearer ${token}`
  }
  return headers
}
