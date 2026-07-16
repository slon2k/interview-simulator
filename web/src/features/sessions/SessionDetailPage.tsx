import { Stack, Text, Title } from '@mantine/core'
import { useParams } from 'react-router-dom'

export function SessionDetailPage() {
  const { sessionId } = useParams()

  return (
    <Stack>
      <Title>Session Detail</Title>
      <Text c="dimmed">Session ID: {sessionId}</Text>
    </Stack>
  )
}
