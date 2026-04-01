"use client"

import type { Seat } from "@/lib/types"
import { cn } from "@/lib/utils"
import { useCart } from "@/context/cart-context"

interface SeatButtonProps {
  seat: Seat
  isSelected: boolean
  onSelect: (seat: Seat) => void
}

const statusConfig = {
  available: {
    bgClass: "bg-seat-available/20 border-seat-available/50 hover:bg-seat-available/40 hover:border-seat-available cursor-pointer",
    label: "Available",
  },
  reserved: {
    bgClass: "bg-seat-reserved/20 border-seat-reserved/50 hover:bg-seat-reserved/30 hover:border-seat-reserved cursor-pointer",
    label: "Reserved",
  },
  sold: {
    bgClass: "bg-seat-sold/20 border-seat-sold/50 hover:bg-seat-sold/30 hover:border-seat-sold cursor-pointer",
    label: "Sold",
  },
} as const

export function SeatButton({ seat, isSelected, onSelect }: SeatButtonProps) {
  const config = statusConfig[seat.status]
  const { isSeatInCart } = useCart()
  const isAvailable = seat.status === "available"
  const isInCart = isAvailable && isSeatInCart(seat.id)
  const isDisabled = isInCart && !isSelected

  const selectedClass = isSelected && (
    isAvailable 
      ? "ring-2 ring-accent bg-accent/30 border-accent"
      : "ring-2 ring-yellow-500 bg-yellow-500/30 border-yellow-500"
  )

  return (
    <button
      type="button"
      disabled={isDisabled}
      onClick={() => onSelect(seat)}
      aria-label={`Seat ${seat.sectionCode}${seat.rowNumber}-${seat.seatNumber}, $${seat.price}, ${config.label}`}
      className={cn(
        "flex items-center justify-center rounded-md border text-xs font-medium transition-all size-10",
        config.bgClass,
        selectedClass
      )}
    >
      {seat.seatNumber}
    </button>
  )
}

export function SeatLegend() {
  return (
    <div className="flex items-center gap-6 text-sm text-muted-foreground">
      <div className="flex items-center gap-2">
        <div className="size-4 rounded-sm bg-seat-available/40 border border-seat-available/60" />
        <span>Available</span>
      </div>
      <div className="flex items-center gap-2">
        <div className="size-4 rounded-sm bg-seat-reserved/40 border border-seat-reserved/60" />
        <span>Reserved</span>
      </div>
      <div className="flex items-center gap-2">
        <div className="size-4 rounded-sm bg-seat-sold/40 border border-seat-sold/60" />
        <span>Sold</span>
      </div>
      <div className="flex items-center gap-2">
        <div className="size-4 rounded-sm ring-2 ring-accent bg-accent/30 border border-accent" />
        <span>Selected</span>
      </div>
    </div>
  )
}
