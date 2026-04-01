"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { format } from "date-fns"
import { ArrowLeft, Calendar, Loader2, Bell, CheckCircle } from "lucide-react"
import { useSeatmap } from "@/hooks/use-seatmap"
import { Seatmap } from "@/components/seatmap"
import { CartSidebar } from "@/components/cart-sidebar"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Badge } from "@/components/ui/badge"
import { useAuth } from "@/context/auth-context"
import { getWaitlistStatus } from "@/lib/api/waitlist"

interface EventDetailClientProps {
  eventId: string
}

interface WaitlistInfo {
  section: string
  queuePosition: number
  status: string
}

export function EventDetailClient({ eventId }: EventDetailClientProps) {
  const { user, isAuthenticated } = useAuth()
  const { data: seatmap, error, isLoading, mutate } = useSeatmap(eventId)
  const [waitlistSections, setWaitlistSections] = useState<WaitlistInfo[]>([])
  const [loadingWaitlist, setLoadingWaitlist] = useState(false)
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    console.log("[EventDetail] User changed, refreshing waitlist. user:", user?.id, user?.email)
    setRefreshKey(k => k + 1)
  }, [user?.id])

  useEffect(() => {
    if (isAuthenticated && user && seatmap?.seats) {
      loadWaitlistStatus()
    } else {
      setWaitlistSections([])
    }
  }, [isAuthenticated, user, seatmap, refreshKey])

  const loadWaitlistStatus = async () => {
    if (!user || !seatmap?.seats) return
    
    console.log("[Waitlist] Loading status for user:", user.id, user.email)
    setLoadingWaitlist(true)
    
    // Check all known sections, not just those in the seatmap
    const sections = ["General", "VIP", "A", "B", "C", "D"]
    const results: WaitlistInfo[] = []
    
    for (const section of sections) {
      try {
        const status = await getWaitlistStatus(eventId, section, user.id)
        console.log("[Waitlist] Section", section, "status:", status)
        // Only include active waitlist entries with valid position
        if (status && status.status === "ACTIVE" && status.queuePosition > 0) {
          results.push({
            section,
            queuePosition: status.queuePosition,
            status: status.status
          })
        }
      } catch (e) {
        console.log("[Waitlist] Section", section, "error:", e)
      }
    }
    
    console.log("[Waitlist] Final results:", results)
    setWaitlistSections(results)
    setLoadingWaitlist(false)
  }

  const uniqueSections = seatmap?.seats 
    ? [...new Set(seatmap.seats.map(s => s.sectionCode))].sort()
    : []

  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto max-w-7xl px-4 py-8">
        {/* Back navigation */}
        <Button variant="ghost" asChild className="mb-6 text-muted-foreground hover:text-foreground">
          <Link href="/">
            <ArrowLeft className="size-4" />
            Back to Events
          </Link>
        </Button>

        <div className="flex flex-col lg:flex-row gap-8">
          {/* Main content */}
          <div className="flex-1 flex flex-col gap-6 min-w-0">
            {/* Event header */}
            {isLoading ? (
              <div className="flex flex-col gap-3">
                <Skeleton className="h-8 w-64" />
                <Skeleton className="h-5 w-96" />
                <div className="flex gap-4">
                  <Skeleton className="h-5 w-40" />
                  <Skeleton className="h-5 w-32" />
                </div>
              </div>
            ) : seatmap ? (
              <div className="flex flex-col gap-2">
                <h1 className="text-3xl font-bold text-foreground tracking-tight">
                  {seatmap.eventName}
                </h1>
                <p className="text-muted-foreground leading-relaxed">
                  {seatmap.eventDescription}
                </p>
                <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                  <div className="flex items-center gap-1.5">
                    <Calendar className="size-4" />
                    <span>
                      {format(
                        new Date(seatmap.eventDate),
                        "EEEE, MMMM d, yyyy 'at' h:mm a"
                      )}
                    </span>
                  </div>
                </div>
              </div>
            ) : null}

            {/* Waitlist Status Banner - Only show if user is on waitlist */}
            {isAuthenticated && user && !isLoading && waitlistSections.length > 0 && (
              <div className="rounded-lg border border-amber-200 bg-amber-50 dark:bg-amber-950/20 dark:border-amber-800 p-4">
                <div className="flex items-center gap-2 mb-3">
                  <Bell className="size-4 text-amber-600" />
                  <span className="font-medium text-amber-900 dark:text-amber-100">
                    You're on the waitlist
                  </span>
                  {loadingWaitlist && <Loader2 className="size-4 animate-spin text-amber-600" />}
                </div>
                <div className="flex flex-wrap gap-2">
                  {waitlistSections.map(info => (
                    <Badge 
                      key={info.section} 
                      variant="outline"
                      className="border-amber-500 text-amber-700 dark:text-amber-300 bg-amber-100/50 dark:bg-amber-900/30"
                    >
                      <CheckCircle className="size-3 mr-1" />
                      Section {info.section}: Position #{info.queuePosition}
                    </Badge>
                  ))}
                </div>
              </div>
            )}

            {/* Seatmap */}
            {isLoading ? (
              <div className="flex flex-col items-center justify-center py-20 gap-3">
                <Loader2 className="size-8 animate-spin text-accent" />
                <p className="text-muted-foreground text-sm">
                  Loading seat map...
                </p>
              </div>
            ) : error ? (
              <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-6 text-center">
                <p className="text-destructive font-medium mb-2">
                  Failed to load seat map
                </p>
                <p className="text-muted-foreground text-sm mb-4">
                  {error.message || "Please check the connection to the Catalog service."}
                </p>
                <Button
                  variant="outline"
                  onClick={() => mutate()}
                  className="border-destructive/30 text-destructive hover:bg-destructive/10"
                >
                  Retry
                </Button>
              </div>
            ) : seatmap ? (
              <Seatmap 
                seatmap={seatmap} 
                eventId={eventId} 
                onSeatReserved={() => mutate()} 
                onWaitlistJoined={loadWaitlistStatus}
              />
            ) : null}
          </div>

          {/* Cart sidebar */}
          <div className="lg:w-80 shrink-0">
            <div className="lg:sticky lg:top-8">
              <CartSidebar />
            </div>
          </div>
        </div>
      </div>
    </main>
  )
}
