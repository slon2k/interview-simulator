import { Stack, Text, Title } from '@mantine/core'

export function LandingPage() {
  return (
    <Stack gap="xl">
      <Stack gap="sm">
        <Title>Practice interviews with AI feedback</Title>
        <Text c="dimmed" size="lg">
          An invite-only AI Interview Simulator for realistic text-based interview practice,
          structured feedback, and progress tracking.
        </Text>
      </Stack>
    </Stack>
  )
}
