import { Button, Group, Stack, Text, Title } from '@mantine/core'
import { Link as RouterLink, useParams } from 'react-router-dom'

export function InterviewDetailPage() {
  const { interviewId } = useParams<{ interviewId: string }>()

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title>Interview</Title>
        <Button component={RouterLink} to="/interviews" variant="light">
          Back to interviews
        </Button>
      </Group>

      <Text c="dimmed">
        Interview detail view for {interviewId ?? 'unknown interview'} will be implemented in 04c.
      </Text>
    </Stack>
  )
}
