import { NextRequest, NextResponse } from "next/server"
import { API_CONFIG } from "@/lib/api/config"

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    const { email, password } = body

    if (!email || !password) {
      return NextResponse.json(
        { message: "Email y contraseña son requeridos" },
        { status: 400 }
      )
    }

    const response = await fetch(`${API_CONFIG.gateway}${API_CONFIG.auth.login}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, password }),
    })

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}))
      return NextResponse.json(
        { message: errorData.detail || "Credenciales inválidas" },
        { status: 401 }
      )
    }

    const data = await response.json()

    const { token, expiresAt, userEmail, userRole } = data

    const payload = JSON.parse(atob(token.split(".")[1]))
    const userId = payload.sub || payload.email

    const nextResponse = NextResponse.json({
      userId,
      email: userEmail,
      role: userRole,
      token,
    })

    nextResponse.cookies.set("auth-token", token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      maxAge: 60 * 60 * 8,
      path: "/",
    })

    return nextResponse
  } catch (error) {
    console.error("Login error:", error)
    return NextResponse.json(
      { message: "Error interno del servidor" },
      { status: 500 }
    )
  }
}
