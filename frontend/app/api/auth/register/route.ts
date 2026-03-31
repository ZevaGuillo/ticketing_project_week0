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

    if (password.length < 8) {
      return NextResponse.json(
        { message: "La contraseña debe tener al menos 8 caracteres" },
        { status: 400 }
      )
    }

    const registerResponse = await fetch(`${API_CONFIG.gateway}${API_CONFIG.auth.register}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ 
        email, 
        password,
        role: "User"
      }),
    })

    if (!registerResponse.ok) {
      const errorData = await registerResponse.json().catch(() => ({}))
      return NextResponse.json(
        { message: errorData.detail || "Error al registrar usuario" },
        { status: registerResponse.status }
      )
    }

    const loginResponse = await fetch(`${API_CONFIG.gateway}${API_CONFIG.auth.login}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, password }),
    })

    if (!loginResponse.ok) {
      return NextResponse.json(
        { message: "Usuario registrado pero error al iniciar sesión" },
        { status: 500 }
      )
    }

    const data = await loginResponse.json()
    const { token, expiresAt, userEmail, userRole } = data

    const payload = JSON.parse(atob(token.split(".")[1]))
    const userId = payload.sub || payload.email

    const nextResponse = NextResponse.json({
      userId,
      email: userEmail,
      role: userRole,
      message: "Usuario registrado exitosamente",
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
    console.error("Register error:", error)
    return NextResponse.json(
      { message: "Error interno del servidor" },
      { status: 500 }
    )
  }
}
