import { Button, Stack, Text, Title } from '@mantine/core'
import { useAuth } from '../features/auth/authContext'

export function UnauthorizedPage() {
  const { logout } = useAuth()

  const handleLogout = () => {
    void logout().then(() => {
      window.location.href = '/'
    })
  }

  return (
    <Stack>
      <Title>Access denied</Title>
      <Text c="dimmed">You are signed in, but your account is not invited to use this app.</Text>
      <Button onClick={handleLogout} variant="light">
        Sign out
      </Button>
    </Stack>
  )
}
