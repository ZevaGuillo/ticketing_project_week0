import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl

  if (pathname.startsWith('/admin')) {
    return handleAdminAuth(request)
  }

  if (pathname.startsWith('/checkout')) {
    return handleCheckoutAuth(request)
  }

  return NextResponse.next()
}

function handleAdminAuth(request: NextRequest) {
  if (request.nextUrl.pathname === '/admin/login') {
    return NextResponse.next()
  }

  const token = request.cookies.get('admin-token')?.value || 
                request.headers.get('authorization')?.replace('Bearer ', '')

  if (!token) {
    return NextResponse.redirect(new URL('/admin/login', request.url))
  }

  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const currentTime = Math.floor(Date.now() / 1000)
    
    if (payload.exp <= currentTime) {
      return NextResponse.redirect(new URL('/admin/login', request.url))
    }

    let role = payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
    
    if (Array.isArray(role)) {
      role = role[0]
    }
    
    if (role !== 'Admin') {
      return NextResponse.redirect(new URL('/admin/login', request.url))
    }

    const response = NextResponse.next()
    response.headers.set('x-admin-token', token)
    
    return response
  } catch {
    return NextResponse.redirect(new URL('/admin/login', request.url))
  }
}

function handleCheckoutAuth(request: NextRequest) {
  const token = request.cookies.get('auth-token')?.value

  if (!token) {
    const loginUrl = new URL('/login', request.url)
    loginUrl.searchParams.set('redirect', pathname)
    return NextResponse.redirect(loginUrl)
  }

  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const currentTime = Math.floor(Date.now() / 1000)
    
    if (payload.exp <= currentTime) {
      const loginUrl = new URL('/login', request.url)
      loginUrl.searchParams.set('redirect', pathname)
      return NextResponse.redirect(loginUrl)
    }

    const response = NextResponse.next()
    response.headers.set('x-user-token', token)
    response.headers.set('x-user-id', payload.sub || payload.email)
    
    return response
  } catch {
    const loginUrl = new URL('/login', request.url)
    loginUrl.searchParams.set('redirect', pathname)
    return NextResponse.redirect(loginUrl)
  }
}

export const config = {
  matcher: [
    '/admin/:path*',
    '/checkout/:path*',
  ]
}
