import { Badge, Card, Code, Group, Stack, Text } from '@mantine/core'
import { useAuth } from './authContext'

export function AuthDebugPanel() {
  const { user, isLoading, error, isAuthenticated, isInvited, isAdmin } = useAuth()

  return (
    <Card withBorder shadow="sm" radius="md">
      <Stack gap="xs">
        <Group justify="space-between">
          <Text fw={600}>Auth debug</Text>
          {isLoading && <Badge color="gray">Loading</Badge>}
          {!isLoading && isAuthenticated && <Badge color="green">Authenticated</Badge>}
          {!isLoading && !isAuthenticated && <Badge color="orange">Anonymous</Badge>}
          {error && <Badge color="red">Error</Badge>}
        </Group>

        {error && (
          <Text size="sm" c="red">
            {error.message} (status {error.status})
          </Text>
        )}

        {!isLoading && (
          <Code block>
            {JSON.stringify({ isAuthenticated, isInvited, isAdmin, user }, null, 2)}
          </Code>
        )}
      </Stack>
    </Card>
  )
}
