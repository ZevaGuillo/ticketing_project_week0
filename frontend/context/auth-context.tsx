"use client"

import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from "react"

interface User {
  id: string
  email: string
  role: string
}

interface AuthContextType {
  userId: string | null
  user: User | null
  token: string | null
  isAuthenticated: boolean
  login: (token: string, user: User) => void
  logout: () => void
  isLoading: boolean
}

const AuthContext = createContext<AuthContextType | null>(null)

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider")
  return ctx
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [userId, setUserId] = useState<string | null>(null)
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    // Check localStorage first (works with httpOnly cookies)
    const storedToken = localStorage.getItem("auth-token")
    const storedUser = localStorage.getItem("auth-user")

    console.log("[AuthContext] Checking storage - token:", !!storedToken, "user:", storedUser)

    if (storedToken && storedUser) {
      try {
        const payload = JSON.parse(atob(storedToken.split(".")[1]))
        const currentTime = Math.floor(Date.now() / 1000)

        console.log("[AuthContext] Token payload:", payload)

        if (payload.exp > currentTime) {
          const extractedUserId = payload.sub || payload.email
          setUserId(extractedUserId)
          setToken(storedToken)
          const parsedUser = JSON.parse(storedUser)
          setUser(parsedUser)
          console.log("[AuthContext] User authenticated:", parsedUser)
        } else {
          localStorage.removeItem("auth-token")
          localStorage.removeItem("auth-user")
        }
      } catch (error) {
        console.error("Error parsing stored token:", error)
      }
    }
    setIsLoading(false)
  }, [])

  const login = useCallback((newToken: string, newUser: User) => {
    console.log("[AuthContext login] newToken:", newToken ? "present" : "empty", "newUser:", newUser)
    localStorage.setItem("auth-token", newToken)
    localStorage.setItem("auth-user", JSON.stringify(newUser))
    setToken(newToken)
    setUserId(newUser.id)
    setUser(newUser)
  }, [])

  const logout = useCallback(() => {
    document.cookie = "auth-token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;"
    localStorage.removeItem("auth-token")
    localStorage.removeItem("auth-user")
    setToken(null)
    setUserId(null)
    setUser(null)
  }, [])

  return (
    <AuthContext.Provider
      value={{
        userId,
        user,
        token,
        isAuthenticated: !!token && !!userId,
        login,
        logout,
        isLoading,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
