import { Alert, Badge, Button, Card, Group, Loader, Stack, Table, Text, Title } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { Link as RouterLink } from 'react-router-dom'
import { ApiError } from '../../api/apiError'
import { getInterviews, type InterviewSummary } from '../../api/interviewApi'

export function InterviewListPage() {
  const interviewsQuery = useQuery({
    queryKey: ['interviews'],
    queryFn: () => getInterviews(),
  })

  if (interviewsQuery.isLoading) {
    return (
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <Title>Interviews</Title>
          <Button component={RouterLink} to="/interviews/new">
            New interview
          </Button>
        </Group>

        <Group>
          <Loader size="sm" />
          <Text c="dimmed">Loading interviews...</Text>
        </Group>
      </Stack>
    )
  }

  if (interviewsQuery.isError) {
    const message =
      interviewsQuery.error instanceof ApiError
        ? interviewsQuery.error.message
        : 'Unable to load interviews.'

    return (
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <Title>Interviews</Title>
          <Button component={RouterLink} to="/interviews/new">
            New interview
          </Button>
        </Group>

        <Alert color="red" title="Could not load interviews">
          {message}
        </Alert>

        <Group>
          <Button variant="light" onClick={() => void interviewsQuery.refetch()}>
            Retry
          </Button>
        </Group>
      </Stack>
    )
  }

  const interviews = interviewsQuery.data ?? []

  if (interviews.length === 0) {
    return (
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <Title>Interviews</Title>
          <Button component={RouterLink} to="/interviews/new">
            New interview
          </Button>
        </Group>

        <Card withBorder radius="md">
          <Stack gap="xs">
            <Text fw={600}>No interviews yet</Text>
            <Text c="dimmed">Start your first interview to begin practicing.</Text>
          </Stack>
        </Card>
      </Stack>
    )
  }

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title>Interviews</Title>
        <Button component={RouterLink} to="/interviews/new">
          New interview
        </Button>
      </Group>

      <Table striped highlightOnHover withTableBorder>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Role</Table.Th>
            <Table.Th>Topic</Table.Th>
            <Table.Th>Seniority</Table.Th>
            <Table.Th>Type</Table.Th>
            <Table.Th>Progress</Table.Th>
            <Table.Th>Status</Table.Th>
            <Table.Th>Action</Table.Th>
          </Table.Tr>
        </Table.Thead>

        <Table.Tbody>
          {interviews.map((interview) => {
            const status = interview.status.toLowerCase()
            const actionLabel =
              status === 'active' ? 'Continue' : status === 'completed' ? 'View' : 'Open'
            const progress = formatProgress(interview)

            return (
              <Table.Tr key={interview.id}>
                <Table.Td>{interview.targetRole}</Table.Td>
                <Table.Td>{interview.focusArea}</Table.Td>
                <Table.Td>{interview.seniorityLevel}</Table.Td>
                <Table.Td>{interview.interviewType}</Table.Td>
                <Table.Td>{progress}</Table.Td>
                <Table.Td>
                  <Badge color={statusColor(status)} variant="light">
                    {interview.status}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Button
                    component={RouterLink}
                    to={`/interviews/${interview.id}`}
                    variant="subtle"
                    size="compact-sm"
                  >
                    {actionLabel}
                  </Button>
                </Table.Td>
              </Table.Tr>
            )
          })}
        </Table.Tbody>
      </Table>
    </Stack>
  )
}

function formatProgress(interview: InterviewSummary): string {
  const answered = toCount(interview.answeredCount)
  const total = toCount(interview.questionCount)

  if (total <= 0) {
    return '0/0'
  }

  return `${answered}/${total}`
}

function toCount(value: number | string): number {
  if (typeof value === 'number') {
    return value
  }

  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) ? parsed : 0
}

function statusColor(status: string): string {
  if (status === 'active') {
    return 'blue'
  }

  if (status === 'completed') {
    return 'green'
  }

  if (status === 'created') {
    return 'gray'
  }

  return 'gray'
}
