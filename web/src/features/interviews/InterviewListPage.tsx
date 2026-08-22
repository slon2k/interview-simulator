import {
  Alert,
  Badge,
  Button,
  Card,
  Group,
  Loader,
  SegmentedControl,
  Stack,
  Table,
  Text,
  Title,
} from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { ApiError } from '../../api/apiError'
import {
  getInterviews,
  type InterviewStatusContract,
  type InterviewSummary,
} from '../../api/interviewApi'
import { formatProgress, statusAction, statusColor } from './interviewListHelpers'

const STATUS_FILTER_OPTIONS: { label: string; value: StatusFilterValue }[] = [
  { label: 'All', value: 'All' },
  { label: 'Created', value: 'Created' },
  { label: 'Active', value: 'Active' },
  { label: 'Completed', value: 'Completed' },
]

type StatusFilterValue = 'All' | InterviewStatusContract

export function InterviewListPage() {
  const [statusFilter, setStatusFilter] = useState<StatusFilterValue>('All')

  const interviewsQuery = useQuery({
    queryKey: ['interviews', { status: statusFilter }],
    queryFn: () => getInterviews(statusFilter === 'All' ? undefined : { status: [statusFilter] }),
  })

  return (
    <Stack gap="md">
      <PageHeader />
      <StatusFilterControl value={statusFilter} onChange={setStatusFilter} />
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

function StatusFilterControl({
  value,
  onChange,
}: {
  value: StatusFilterValue
  onChange: (value: StatusFilterValue) => void
}) {
  return (
    <SegmentedControl
      value={value}
      onChange={onChange}
      data={STATUS_FILTER_OPTIONS}
      aria-label="Filter interviews by status"
    />
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
  return (
    <Table.Tr>
      <Table.Td>{interview.targetRole}</Table.Td>
      <Table.Td>{interview.focusArea}</Table.Td>
      <Table.Td>{interview.seniorityLevel}</Table.Td>
      <Table.Td>{interview.interviewType}</Table.Td>
      <Table.Td>{formatProgress(interview)}</Table.Td>
      <Table.Td>
        <Badge color={statusColor(interview.status)} variant="light">
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
          {statusAction(interview.status)}
        </Button>
      </Table.Td>
    </Table.Tr>
  )
}
