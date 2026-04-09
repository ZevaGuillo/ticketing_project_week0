"use client"

import { useMemo, useState, useCallback } from "react"
import type { Seat, SeatmapResponse } from "@/lib/types"
import { SeatButton, SeatLegend } from "@/components/seat-button"
import { useCart } from "@/context/cart-context"
import { Loader2, Bell } from "lucide-react"
import { Button } from "@/components/ui/button"
import { joinWaitlist, type UserOpportunity } from "@/lib/api/waitlist"
import { useAuth } from "@/context/auth-context"

interface SeatmapProps {
  seatmap: SeatmapResponse
  eventId: string
  onSeatReserved?: () => void
  onWaitlistJoined?: (section: string) => void
  opportunities?: UserOpportunity[]
  joinedWaitlistSections?: string[]
}

export function Seatmap({ seatmap, eventId, onSeatReserved, onWaitlistJoined, opportunities = [], joinedWaitlistSections = [] }: SeatmapProps) {
  const { user } = useAuth()
  const { reserveSeatAndAddToCart, isAddingToCart, isSeatInCart, removeSeatFromCart } = useCart()
  const [selectedSeat, setSelectedSeat] = useState<Seat | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)
  const [joiningSection, setJoiningSection] = useState<string | null>(null)
  const [joinedSections, setJoinedSections] = useState<Set<string>>(new Set())

  const opportunitiesBySeatId = useMemo(() => {
    const map = new Map<string, UserOpportunity>()
    for (const opp of opportunities) {
      map.set(opp.seatId, opp)
    }
    return map
  }, [opportunities])

  // Group seats by section, then by row
  const sections = useMemo(() => {
    const grouped = new Map<string, Map<number, Seat[]>>()

    for (const seat of seatmap.seats) {
      if (!grouped.has(seat.sectionCode)) {
        grouped.set(seat.sectionCode, new Map())
      }
      const sectionRows = grouped.get(seat.sectionCode)!
      if (!sectionRows.has(seat.rowNumber)) {
        sectionRows.set(seat.rowNumber, [])
      }
      sectionRows.get(seat.rowNumber)!.push(seat)
    }

    // Sort seats within each row
    for (const sectionRows of grouped.values()) {
      for (const [rowNum, seats] of sectionRows) {
        sectionRows.set(
          rowNum,
          seats.sort((a, b) => a.seatNumber - b.seatNumber)
        )
      }
    }

    return grouped
  }, [seatmap.seats])

  const handleSelect = useCallback((seat: Seat) => {
    if (isSeatInCart(seat.id)) {
      setSelectedSeat(null)
      return
    }
    setSelectedSeat((prev) => (prev?.id === seat.id ? null : seat))
    setLocalError(null)
  }, [isSeatInCart])

  const handleReserve = useCallback(async () => {
    if (!selectedSeat) return
    
    if (isSeatInCart(selectedSeat.id)) {
      removeSeatFromCart(selectedSeat.id)
      setSelectedSeat(null)
      return
    }
    
    setLocalError(null)

    try {
      // Check if user has an opportunity for this seat
      const opportunity = opportunitiesBySeatId.get(selectedSeat.id)
      
      await reserveSeatAndAddToCart(selectedSeat, eventId, seatmap.eventName, opportunity?.token)
      setSelectedSeat(null)
      onSeatReserved?.()
    } catch (err) {
      setLocalError(
        err instanceof Error ? err.message : "Failed to reserve seat"
      )
    }
  }, [selectedSeat, reserveSeatAndAddToCart, isSeatInCart, removeSeatFromCart, onSeatReserved, opportunitiesBySeatId, eventId])

  const handleJoinWaitlistForSection = useCallback(async (section: string) => {
    if (!user) return

    setLocalError(null)
    setJoiningSection(section)

    try {
      await joinWaitlist({ eventId, section }, user.id)
      setJoinedSections(prev => new Set(prev).add(section))
      onWaitlistJoined?.(section)
    } catch (err) {
      setLocalError(
        err instanceof Error ? err.message : "Failed to join waitlist"
      )
    } finally {
      setJoiningSection(null)
    }
  }, [user, eventId, onWaitlistJoined])

  // A section is "full" when every single seat is reserved or sold
  const fullSections = useMemo(() => {
    const full = new Set<string>()
    const sectionsWithOpportunity = new Set(opportunities.map(o => o.section))
    for (const [sectionCode, rows] of sections.entries()) {
      const allSeats = Array.from(rows.values()).flat()
      const userHasSeatHere = allSeats.some(s => isSeatInCart(s.id))
      const userHasOpportunityHere = sectionsWithOpportunity.has(sectionCode)
      if (!userHasSeatHere && !userHasOpportunityHere && allSeats.length > 0 && allSeats.every(s => s.status !== "available")) {
        full.add(sectionCode)
      }
    }
    return full
  }, [sections, isSeatInCart, opportunities])

  const hasOpportunityForSelected = selectedSeat ? opportunitiesBySeatId.has(selectedSeat.id) : false
  const isUnavailable = selectedSeat && selectedSeat.status !== "available" && !hasOpportunityForSelected

  return (
    <div className="flex flex-col gap-6">
      {/* Stage visual */}
      <div className="flex justify-center">
        <div className="bg-secondary rounded-lg px-16 py-3 text-muted-foreground text-sm font-medium tracking-widest uppercase">
          Stage
        </div>
      </div>

      {/* Seat grid */}
      <div className="flex flex-col gap-6">
        {Array.from(sections.entries())
          .sort(([a], [b]) => a.localeCompare(b))
          .map(([sectionCode, rows]) => (
            <div key={sectionCode} className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-accent uppercase tracking-wider">
                  Section {sectionCode}
                </h3>
                {fullSections.has(sectionCode) && user && (() => {
                  const alreadyJoined = joinedSections.has(sectionCode) || joinedWaitlistSections.includes(sectionCode)
                  const isJoining = joiningSection === sectionCode
                  return (
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-7 text-xs border-amber-500 text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-950/20"
                      onClick={() => !alreadyJoined && handleJoinWaitlistForSection(sectionCode)}
                      disabled={isJoining || alreadyJoined}
                    >
                      {isJoining ? (
                        <><Loader2 className="size-3 animate-spin" />Joining...</>
                      ) : alreadyJoined ? (
                        <><Bell className="size-3" />On Waitlist</>
                      ) : (
                        <><Bell className="size-3" />Join Waitlist</>
                      )}
                    </Button>
                  )
                })()}
              </div>
              <div className="flex flex-col gap-1.5">
                {Array.from(rows.entries())
                  .sort(([a], [b]) => a - b)
                  .map(([rowNumber, seats]) => (
                    <div key={rowNumber} className="flex items-center gap-2">
                      <span className="text-xs text-muted-foreground w-8 text-right font-mono shrink-0">
                        R{rowNumber}
                      </span>
                      <div className="flex items-center gap-1.5 flex-wrap">
                        {seats.map((seat) => {
                          const isInUserCart = isSeatInCart(seat.id)
                          const displaySeat = isInUserCart
                            ? { ...seat, status: "reserved" as const }
                            : seat
                          const isOffered = opportunitiesBySeatId.has(seat.id)

                          return (
                            <SeatButton
                              key={seat.id}
                              seat={displaySeat}
                              isSelected={selectedSeat?.id === seat.id}
                              onSelect={handleSelect}
                              isOffered={isOffered}
                            />
                          )
                        })}
                      </div>
                    </div>
                  ))}
              </div>
            </div>
          ))}
      </div>

      {/* Legend */}
      <SeatLegend />

      {/* Selected seat action */}
      {selectedSeat && (
        <div className="flex items-center justify-between rounded-lg border border-accent/30 bg-accent/5 p-4">
          <div className="flex flex-col gap-0.5">
            <p className="text-sm font-medium text-foreground">
              Section {selectedSeat.sectionCode}, Row{" "}
              {selectedSeat.rowNumber}, Seat {selectedSeat.seatNumber}
            </p>
            <p className="text-lg font-semibold text-accent">
              ${selectedSeat.price.toFixed(2)}
            </p>
            {isSeatInCart(selectedSeat.id) && (
              <p className="text-xs text-accent mt-1">Already in your cart</p>
            )}
            {selectedSeat.status === "reserved" && !hasOpportunityForSelected && (
              <p className="text-xs text-amber-600 mt-1">Seat reserved by another user</p>
            )}
            {hasOpportunityForSelected && (
              <p className="text-xs text-green-600 mt-1">¡Tienes una oportunidad para este asiento!</p>
            )}
            {selectedSeat.status === "sold" && (
              <p className="text-xs text-red-600 mt-1">Seat already sold</p>
            )}
          </div>
          <div className="flex gap-2">
            {(selectedSeat.status === "available" || hasOpportunityForSelected) && (
              <Button
                onClick={handleReserve}
                disabled={isAddingToCart}
                variant={isSeatInCart(selectedSeat.id) ? "destructive" : "default"}
                className={isSeatInCart(selectedSeat.id) ? "" : "bg-accent text-accent-foreground hover:bg-accent/90"}
              >
                {isAddingToCart ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    {isSeatInCart(selectedSeat.id) ? "Removing..." : "Reserving..."}
                  </>
                ) : isSeatInCart(selectedSeat.id) ? (
                  "Remove from Cart"
                ) : (
                  "Reserve & Add to Cart"
                )}
              </Button>
            )}
          </div>
        </div>
      )}

      {/* Error */}
      {localError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          {localError}
        </div>
      )}
    </div>
  )
}
