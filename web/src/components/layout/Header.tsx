import { AppShell, Button, Container, Group, Title } from '@mantine/core'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import { useAuth } from '../../features/auth/authContext'
import { buildLoginUrl } from '../../features/auth/authApi'

const navItems = [
  { label: 'Home', to: '/' },
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Interview', to: '/interview/new' },
  { label: 'History', to: '/history' },
]

export function Header() {
  const location = useLocation()
  const { isAuthenticated, logout } = useAuth()

  function isActive(to: string) {
    if (to === '/') {
      return location.pathname === '/'
    }
    return location.pathname.startsWith(to)
  }

  const handleLogin = () => {
    window.location.href = buildLoginUrl(window.location.pathname)
  }

  const handleLogout = () => {
    void logout().then(() => {
      window.location.href = '/'
    })
  }

  return (
    <AppShell.Header>
      <Container size="lg" h="100%">
        <Group h="100%" justify="space-between" wrap="nowrap">
          <Title order={3} textWrap="nowrap">
            AI Interview Simulator
          </Title>

          <Group gap="xs" wrap="nowrap">
            {navItems.map((item) => (
              <Button
                key={item.to}
                component={RouterLink}
                to={item.to}
                variant={isActive(item.to) ? 'light' : 'subtle'}
                color={isActive(item.to) ? 'blue' : 'gray'}
              >
                {item.label}
              </Button>
            ))}

            {!isAuthenticated && (
              <Button onClick={handleLogin} variant="filled">
                Sign in with GitHub
              </Button>
            )}
            {isAuthenticated && (
              <Button onClick={handleLogout} variant="outline" color="gray">
                Logout
              </Button>
            )}
          </Group>
        </Group>
      </Container>
    </AppShell.Header>
  )
}
