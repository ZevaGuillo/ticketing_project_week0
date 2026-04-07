import { NextRequest, NextResponse } from "next/server"
import { API_CONFIG } from "@/lib/api/config"

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    let { email, password } = body

    if (!email || !password) {
      return NextResponse.json(
        { message: "Email and password are required" },
        { status: 400 }
      )
    }

    password = password
      .replace(/\\041/g, '!')
      .replace(/\\044/g, '$')
      .replace(/\\040/g, ' ')
      .replace(/\\035/g, '#')
      .replace(/\\046/g, '&')

    try {
      const controller = new AbortController()
      const timeoutId = setTimeout(() => controller.abort(), 5000)
      
      const healthCheck = await fetch(`${API_CONFIG.gateway}/health`, { 
        method: "GET",
        signal: controller.signal
      })
      
      clearTimeout(timeoutId)
      
      if (!healthCheck.ok) {
        return NextResponse.json(
          { 
            message: "Servicio de autenticación no disponible", 
            debug: `Gateway health check failed`
          },
          { status: 503 }
        )
      }
    } catch (healthError) {
      return NextResponse.json(
        { 
          message: "Servicio de autenticación no disponible", 
          debug: `Cannot reach Gateway at ${API_CONFIG.gateway}`
        },
        { status: 503 }
      )
    }

    const identityResponse = await fetch(`${API_CONFIG.gateway}/auth/token`, {
      method: "POST", 
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email,
        password,
      }),
    })

    if (!identityResponse.ok) {
      const responseText = await identityResponse.text()
      let errorData = null
      try {
        errorData = JSON.parse(responseText)
      } catch {
        errorData = { message: responseText || "Error desconocido del servicio de autenticación" }
      }
      
      return NextResponse.json(
        { 
          message: errorData?.message || "Credenciales inválidas",
          debug: `Gateway returned ${identityResponse.status}: ${responseText}`
        },
        { status: 401 }
      )
    }

    const { token, userRole, userEmail, expiresAt } = await identityResponse.json()

    if (userRole !== "Admin") {
      return NextResponse.json(
        { message: "Acceso no autorizado. Se requiere rol de administrador." },
        { status: 403 }
      )
    }

    try {
      const payload = JSON.parse(atob(token.split(".")[1]))
      
      let jwtRole = payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
      
      if (Array.isArray(jwtRole)) {
        jwtRole = jwtRole[0]
      }
      
      if (jwtRole !== "Admin") {
        return NextResponse.json(
          { 
            message: "Token inválido - rol no coincide.",
            debug: `Expected 'Admin', got '${jwtRole}'`
          },
          { status: 403 }
        )
      }
    } catch (error) {
      return NextResponse.json(
        { message: "Token inválido" },
        { status: 401 }
      )
    }

    const response = NextResponse.json({ 
      token, 
      userRole, 
      userEmail,
      expiresAt 
    })
    
    response.cookies.set("admin-token", token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      maxAge: 60 * 60 * 8,
      path: "/admin"
    })

    return response
    
  } catch (error) {
    console.error("Admin login error:", error)
    return NextResponse.json(
      { message: "Error interno del servidor" },
      { status: 500 }
    )
  }
}
