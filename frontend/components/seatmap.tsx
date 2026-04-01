"use client"

import { useMemo, useState, useCallback } from "react"
import type { Seat, SeatmapResponse } from "@/lib/types"
import { SeatButton, SeatLegend } from "@/components/seat-button"
import { useCart } from "@/context/cart-context"
import { Loader2, Bell } from "lucide-react"
import { Button } from "@/components/ui/button"
import { joinWaitlist } from "@/lib/api/waitlist"
import { useAuth } from "@/context/auth-context"

interface SeatmapProps {
  seatmap: SeatmapResponse
  eventId: string
  onSeatReserved?: () => void
  onWaitlistJoined?: (section: string) => void
}

export function Seatmap({ seatmap, eventId, onSeatReserved, onWaitlistJoined }: SeatmapProps) {
  const { user } = useAuth()
  const { reserveSeatAndAddToCart, isAddingToCart, isSeatInCart, removeSeatFromCart } = useCart()
  const [selectedSeat, setSelectedSeat] = useState<Seat | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)
  const [joiningWaitlist, setJoiningWaitlist] = useState<string | null>(null)
  const [waitlistJoined, setWaitlistJoined] = useState<string | null>(null)

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
    setWaitlistJoined(null)
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
      await reserveSeatAndAddToCart(selectedSeat)
      setSelectedSeat(null)
      onSeatReserved?.()
    } catch (err) {
      setLocalError(
        err instanceof Error ? err.message : "Failed to reserve seat"
      )
    }
  }, [selectedSeat, reserveSeatAndAddToCart, isSeatInCart, removeSeatFromCart, onSeatReserved])

  const handleJoinWaitlist = useCallback(async () => {
    if (!selectedSeat || !user) return
    
    setLocalError(null)
    setJoiningWaitlist(selectedSeat.id)

    try {
      await joinWaitlist(
        { eventId, section: selectedSeat.sectionCode },
        user.id
      )
      setWaitlistJoined(selectedSeat.id)
      setSelectedSeat(null)
      onWaitlistJoined?.(selectedSeat.sectionCode)
    } catch (err) {
      setLocalError(
        err instanceof Error ? err.message : "Failed to join waitlist"
      )
    } finally {
      setJoiningWaitlist(null)
    }
  }, [selectedSeat, user, eventId, onWaitlistJoined])

  const isUnavailable = selectedSeat && selectedSeat.status !== "available"

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
              <h3 className="text-sm font-semibold text-accent uppercase tracking-wider">
                Section {sectionCode}
              </h3>
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

                          return (
                            <SeatButton
                              key={seat.id}
                              seat={displaySeat}
                              isSelected={selectedSeat?.id === seat.id}
                              onSelect={handleSelect}
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
            {selectedSeat.status === "reserved" && (
              <p className="text-xs text-amber-600 mt-1">Seat reserved by another user</p>
            )}
            {selectedSeat.status === "sold" && (
              <p className="text-xs text-red-600 mt-1">Seat already sold</p>
            )}
          </div>
          <div className="flex gap-2">
            {isUnavailable && user && (
              <Button
                onClick={handleJoinWaitlist}
                disabled={joiningWaitlist === selectedSeat.id || waitlistJoined === selectedSeat.id}
                variant="outline"
                className="border-amber-500 text-amber-600 hover:bg-amber-50"
              >
                {joiningWaitlist === selectedSeat.id ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    Joining...
                  </>
                ) : waitlistJoined === selectedSeat.id ? (
                  <>
                    <Bell className="size-4" />
                    Joined Waitlist
                  </>
                ) : (
                  <>
                    <Bell className="size-4" />
                    Join Waitlist
                  </>
                )}
              </Button>
            )}
            {selectedSeat.status === "available" && (
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
