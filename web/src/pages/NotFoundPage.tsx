import { Button, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <Stack>
      <Title>Page not found</Title>
      <Text c="dimmed">The page you requested does not exist.</Text>
      <Button component={Link} to="/">
        Go home
      </Button>
    </Stack>
  )
}
