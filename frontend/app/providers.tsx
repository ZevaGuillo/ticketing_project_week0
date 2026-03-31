"use client"

import { SWRConfig } from "swr"
import { usePathname, useRouter } from "next/navigation" 
import { AuthProvider, useAuth } from "@/context/auth-context"
import { AdminAuthProvider } from "@/context/admin-auth-context"
import { CartProvider } from "@/context/cart-context"
import { Navbar } from "@/components/navbar"
import type { ReactNode } from "react"

function AuthenticatedApp({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()
  const pathname = usePathname()
  const router = useRouter()

  console.log("[AuthenticatedApp] isLoading:", isLoading, "isAuthenticated:", isAuthenticated)

  // Don't apply regular auth to admin routes or auth pages
  if (pathname?.startsWith('/admin') || pathname === '/login' || pathname === '/register') {
    return (
      <CartProvider>
        <Navbar />
        {children}
      </CartProvider>
    )
  }

  return (
    <CartProvider>
      <Navbar />
      {children}
    </CartProvider>
  )
}

function ConditionalAuthProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname()

  // Use AdminAuthProvider for admin routes
  if (pathname?.startsWith('/admin')) {
    return (
      <AdminAuthProvider>
        {children}
      </AdminAuthProvider>
    )
  }

  // Use regular AuthProvider for user routes
  return (
    <AuthProvider>
      <AuthenticatedApp>{children}</AuthenticatedApp>
    </AuthProvider>
  )
}

export function Providers({ children }: { children: ReactNode }) {
  return (
    <SWRConfig
      value={{
        revalidateOnFocus: false,
        errorRetryCount: 2,
      }}
    >
      <ConditionalAuthProvider>
        {children}
      </ConditionalAuthProvider>
    </SWRConfig>
  )
}
