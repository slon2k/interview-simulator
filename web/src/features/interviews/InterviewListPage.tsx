import { Alert, Badge, Button, Card, Group, Loader, Stack, Table, Text, Title } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { Link as RouterLink } from 'react-router-dom'
import { ApiError } from '../../api/apiError'
import { getInterviews, type InterviewSummary } from '../../api/interviewApi'
import { formatProgress, statusAction, statusColor } from './interviewListHelpers'

export function InterviewListPage() {
  const interviewsQuery = useQuery({
    queryKey: ['interviews'],
    queryFn: () => getInterviews(),
  })

  return (
    <Stack gap="md">
      <PageHeader />
      <InterviewListBody query={interviewsQuery} />
    </Stack>
  )
}

function PageHeader() {
  return (
    <Group justify="space-between" align="center">
      <Title>Interviews</Title>
      <Button component={RouterLink} to="/interviews/new">
        New interview
      </Button>
    </Group>
  )
}

type InterviewsQuery = ReturnType<typeof useQuery<InterviewSummary[]>>

function InterviewListBody({ query }: { query: InterviewsQuery }) {
  if (query.isLoading) {
    return (
      <Group>
        <Loader size="sm" />
        <Text c="dimmed">Loading interviews...</Text>
      </Group>
    )
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError ? query.error.message : 'Unable to load interviews.'

    return (
      <>
        <Alert color="red" title="Could not load interviews">
          {message}
        </Alert>
        <Group>
          <Button variant="light" onClick={() => void query.refetch()}>
            Retry
          </Button>
        </Group>
      </>
    )
  }

  const interviews = query.data ?? []

  if (interviews.length === 0) {
    return <EmptyState />
  }

  return <InterviewsTable interviews={interviews} />
}

function EmptyState() {
  return (
    <Card withBorder radius="md">
      <Stack gap="xs">
        <Text fw={600}>No interviews yet</Text>
        <Text c="dimmed">Start your first interview to begin practicing.</Text>
      </Stack>
    </Card>
  )
}

function InterviewsTable({ interviews }: { interviews: InterviewSummary[] }) {
  return (
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
        {interviews.map((interview) => (
          <InterviewRow key={interview.id} interview={interview} />
        ))}
      </Table.Tbody>
    </Table>
  )
}

function InterviewRow({ interview }: { interview: InterviewSummary }) {
  const status = interview.status.toLowerCase()

  return (
    <Table.Tr>
      <Table.Td>{interview.targetRole}</Table.Td>
      <Table.Td>{interview.focusArea}</Table.Td>
      <Table.Td>{interview.seniorityLevel}</Table.Td>
      <Table.Td>{interview.interviewType}</Table.Td>
      <Table.Td>{formatProgress(interview)}</Table.Td>
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
          {statusAction(status)}
        </Button>
      </Table.Td>
    </Table.Tr>
  )
}
