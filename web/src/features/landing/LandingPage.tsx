import { Badge, Button, Card, Group, Stack, Text, Title } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { getHealth } from '../../api/healthApi'
import { AuthDebugPanel } from '../auth/AuthDebugPanel'

export function LandingPage() {
  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: getHealth,
  })

  return (
    <Stack gap="xl">
      <Stack gap="sm">
        <Title>Practice interviews with AI feedback</Title>
        <Text c="dimmed" size="lg">
          An invite-only AI Interview Simulator for realistic text-based interview practice,
          structured feedback, and progress tracking.
        </Text>

        <Group>
          <Button component="a" href="/login">
            Sign in with GitHub
          </Button>
          <Button variant="light" component="a" href="/api/health">
            Check API Health
          </Button>
        </Group>
      </Stack>

      <Card withBorder shadow="sm" radius="md">
        <Stack gap="xs">
          <Group justify="space-between">
            <Text fw={600}>API status</Text>

            {healthQuery.isLoading && <Badge color="gray">Checking</Badge>}
            {healthQuery.isError && <Badge color="red">Unavailable</Badge>}
            {healthQuery.data && <Badge color="green">Online</Badge>}
          </Group>

          <Text size="sm" c="dimmed">
            {healthQuery.data
              ? `Health endpoint status returned: ${healthQuery.status}`
              : 'The frontend will call /api/health through the API client.'}
          </Text>
        </Stack>
      </Card>

      <AuthDebugPanel />
    </Stack>
  )
}
