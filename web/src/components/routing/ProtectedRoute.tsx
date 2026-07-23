import { useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Center, Loader, Stack, Text } from '@mantine/core'
import { useAuth } from '../../features/auth/authContext'
import { buildLoginUrl } from '../../features/auth/authApi'

interface ProtectedRouteProps {
  children: React.ReactNode
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, isInvited, isAdmin, isLoading } = useAuth()

  useEffect(() => {
    if (isLoading) {
      return // Still fetching auth state, don't navigate yet
    }

    if (!isAuthenticated) {
      // Anonymous user: redirect to GitHub OAuth with return URL
      window.location.href = buildLoginUrl(location.pathname)
      return
    }

    if (!isInvited && !isAdmin) {
      // Authenticated but not invited: show access denied
      void navigate('/unauthorized', { replace: true })
      return
    }
  }, [isLoading, isAuthenticated, isInvited, isAdmin, navigate, location.pathname])

  // Show loading UI while fetching auth state
  if (isLoading) {
    return (
      <Center h={400}>
        <Stack align="center" gap="md">
          <Loader />
          <Text c="dimmed">Loading...</Text>
        </Stack>
      </Center>
    )
  }

  // Don't render children until auth checks complete
  if (!isAuthenticated || (!isInvited && !isAdmin)) {
    return null
  }

  return <>{children}</>
}
