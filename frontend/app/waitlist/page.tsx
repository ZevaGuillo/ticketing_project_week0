"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { format } from "date-fns"
import { ArrowLeft, Loader2, Calendar, MapPin, X, AlertCircle } from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { getAllWaitlistStatus, cancelWaitlist, type WaitlistStatusResponse } from "@/lib/api/waitlist"
import { getEvents } from "@/lib/api/catalog"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"

interface WaitlistEntryWithEvent extends WaitlistStatusResponse {
  eventName: string
}

export default function WaitlistPage() {
  const { user, isAuthenticated, isLoading: authLoading } = useAuth()
  const [waitlistEntries, setWaitlistEntries] = useState<WaitlistEntryWithEvent[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [cancellingId, setCancellingId] = useState<string | null>(null)

  useEffect(() => {
    if (!authLoading && isAuthenticated && user) {
      loadWaitlist()
    }
  }, [authLoading, isAuthenticated, user])

  const loadWaitlist = async () => {
    if (!user) return
    
    setIsLoading(true)
    setError(null)
    
    try {
      console.log("[WaitlistPage] Loading for user:", user.id)
      const events = await getEvents()
      const allEntries: WaitlistEntryWithEvent[] = []

      for (const event of events) {
        const entries = await getAllWaitlistStatus(event.id, user.id)
        for (const entry of entries) {
          allEntries.push({ ...entry, eventName: event.name })
        }
      }

      console.log("[WaitlistPage] Loaded entries:", allEntries)
      setWaitlistEntries(allEntries)
    } catch (err) {
      console.error("[WaitlistPage] Load error:", err)
      setError(err instanceof Error ? err.message : "Failed to load waitlist")
    } finally {
      setIsLoading(false)
    }
  }

  const handleCancel = async (entry: WaitlistEntryWithEvent) => {
    if (!user) return
    
    console.log("[WaitlistPage] Cancelling entry:", entry)
    setCancellingId(entry.waitlistEntryId)
    try {
      await cancelWaitlist(entry.eventId, entry.section, user.id)
      console.log("[WaitlistPage] Cancelled successfully, removing from list")
      setWaitlistEntries(prev => prev.filter(e => e.waitlistEntryId !== entry.waitlistEntryId))
    } catch (err) {
      console.error("[WaitlistPage] Cancel error:", err)
      setError(err instanceof Error ? err.message : "Failed to cancel waitlist")
    } finally {
      setCancellingId(null)
    }
  }

  if (authLoading) {
    return (
      <div className="min-h-screen bg-background">
        <div className="mx-auto max-w-4xl px-4 py-8">
          <Skeleton className="h-8 w-48 mb-8" />
          <div className="space-y-4">
            {[1, 2, 3].map(i => (
              <Skeleton key={i} className="h-32 w-full" />
            ))}
          </div>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-background">
        <div className="mx-auto max-w-4xl px-4 py-8">
          <Button variant="ghost" asChild className="mb-6 text-muted-foreground hover:text-foreground">
            <Link href="/">
              <ArrowLeft className="size-4" />
              Back to Events
            </Link>
          </Button>
          <div className="text-center py-12">
            <AlertCircle className="size-12 mx-auto text-muted-foreground mb-4" />
            <h2 className="text-xl font-semibold mb-2">Login Required</h2>
            <p className="text-muted-foreground mb-4">Please login to view your waitlist subscriptions.</p>
            <Button asChild>
              <Link href="/login">Login</Link>
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-4xl px-4 py-8">
        <Button variant="ghost" asChild className="mb-6 text-muted-foreground hover:text-foreground">
          <Link href="/">
            <ArrowLeft className="size-4" />
            Back to Events
          </Link>
        </Button>

        <div className="flex items-center gap-3 mb-8">
          <div className="p-2 bg-amber-100 rounded-lg dark:bg-amber-900/30">
            <AlertCircle className="size-6 text-amber-600 dark:text-amber-400" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight">My Waitlist</h1>
            <p className="text-muted-foreground">Manage your waitlist subscriptions</p>
          </div>
        </div>

        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive mb-4">
            {error}
          </div>
        )}

        {isLoading ? (
          <div className="space-y-4">
            {[1, 2, 3].map(i => (
              <Skeleton key={i} className="h-32 w-full" />
            ))}
          </div>
        ) : waitlistEntries.length === 0 ? (
          <div className="text-center py-12">
            <AlertCircle className="size-12 mx-auto text-muted-foreground mb-4" />
            <h2 className="text-xl font-semibold mb-2">No Active Waitlist</h2>
            <p className="text-muted-foreground mb-4">You are not on any waitlist yet.</p>
            <Button asChild>
              <Link href="/">Browse Events</Link>
            </Button>
          </div>
        ) : (
          <div className="space-y-4">
            {waitlistEntries.map((entry) => (
              <Card key={entry.waitlistEntryId} className="border-amber-200 dark:border-amber-800">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <div className="flex items-center gap-3">
                    <MapPin className="size-4 text-muted-foreground" />
                    <div>
                      <CardTitle className="text-lg">{entry.eventName}</CardTitle>
                      <CardDescription>Section {entry.section}</CardDescription>
                    </div>
                    <Badge 
                      variant={entry.status === "ACTIVE" ? "default" : "secondary"}
                      className={entry.status === "ACTIVE" ? "bg-amber-500" : ""}
                    >
                      {entry.status}
                    </Badge>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleCancel(entry)}
                    disabled={cancellingId === entry.waitlistEntryId}
                    className="text-muted-foreground hover:text-destructive"
                  >
                    {cancellingId === entry.waitlistEntryId ? (
                      <Loader2 className="size-4 animate-spin" />
                    ) : (
                      <X className="size-4" />
                    )}
                    <span className="sr-only">Cancel waitlist</span>
                  </Button>
                </CardHeader>
                <CardContent>
                  <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
                    <div className="flex items-center gap-1.5">
                      <Calendar className="size-4" />
                      <span>Joined {format(new Date(entry.joinedAt), "MMM d, yyyy 'at' h:mm a")}</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <span className="font-medium text-foreground">Position #{entry.queuePosition}</span>
                    </div>
                  </div>
                  <p className="text-sm text-amber-600 mt-2">
                    You will be notified when a seat becomes available in this section.
                  </p>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}