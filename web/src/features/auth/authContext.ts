import { createContext, useContext } from 'react'
import type { ApiError } from '../../api/apiError'
import type { CurrentUser } from './authTypes'

export type AuthContextValue = {
  user: CurrentUser | null
  isLoading: boolean
  error: ApiError | null

  isAuthenticated: boolean
  isInvited: boolean
  isAdmin: boolean

  refreshCurrentUser: () => Promise<CurrentUser>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (context === null) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
