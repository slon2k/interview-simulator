import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { toApiError, type ApiError } from '../../api/apiError'
import { getCurrentUser, logoutCurrentUser } from './authApi'
import { AuthContext, type AuthContextValue } from './authContext'
import type { CurrentUser } from './authTypes'

type AuthState = {
  user: CurrentUser | null
  isLoading: boolean
  error: ApiError | null
}

type AuthProviderProps = {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [{ user, isLoading, error }, setAuthState] = useState<AuthState>({
    user: null,
    isLoading: true,
    error: null,
  })

  useEffect(() => {
    const controller = new AbortController()

    getCurrentUser(controller.signal)
      .then((currentUser) => {
        setAuthState({ user: currentUser, isLoading: false, error: null })
      })
      .catch((err: unknown) => {
        if ((err as { name?: string }).name === 'CanceledError') return
        setAuthState({ user: null, isLoading: false, error: toApiError(err) })
      })

    return () => {
      controller.abort()
    }
  }, [])

  const refreshCurrentUser = useCallback(async (): Promise<CurrentUser> => {
    setAuthState((prev) => ({ ...prev, isLoading: true, error: null }))
    try {
      const currentUser = await getCurrentUser()
      setAuthState({ user: currentUser, isLoading: false, error: null })
      return currentUser
    } catch (err) {
      const apiError = toApiError(err)
      setAuthState((prev) => ({ ...prev, isLoading: false, error: apiError }))
      throw apiError
    }
  }, [])

  const logout = useCallback(async (): Promise<void> => {
    setAuthState((prev) => ({ ...prev, error: null }))

    try {
      await logoutCurrentUser()
      setAuthState({ user: null, isLoading: false, error: null })
    } catch (err) {
      const apiError = toApiError(err)
      setAuthState((prev) => ({ ...prev, isLoading: false, error: apiError }))
      throw apiError
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isLoading,
      error,
      isAuthenticated: user?.isAuthenticated ?? false,
      isInvited: user?.isInvited ?? false,
      isAdmin: user?.isAdmin ?? false,
      refreshCurrentUser,
      logout,
    }),
    [user, isLoading, error, refreshCurrentUser, logout]
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
