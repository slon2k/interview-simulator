import {
  Alert,
  Badge,
  Button,
  Card,
  Center,
  Group,
  Loader,
  SimpleGrid,
  Stack,
  Table,
  Text,
  Title,
} from '@mantine/core'
import { BarChart, LineChart } from '@mantine/charts'
import { useQuery } from '@tanstack/react-query'
import { Link as RouterLink } from 'react-router-dom'
import { ApiError } from '../../api/apiError'
import {
  getDashboard,
  type DashboardResponse,
  type DashboardScoreTrendPointResponse,
} from '../../api/dashboardApi'

export function DashboardPage() {
  const dashboardQuery = useQuery({
    queryKey: ['dashboard'],
    queryFn: () => getDashboard(),
  })

  return (
    <Stack gap="md">
      <PageHeader />
      <DashboardBody query={dashboardQuery} />
    </Stack>
  )
}

function PageHeader() {
  return <Title>Dashboard</Title>
}

type DashboardQuery = ReturnType<typeof useQuery<DashboardResponse>>

function DashboardBody({ query }: { query: DashboardQuery }) {
  if (query.isLoading) {
    return (
      <Center h={400}>
        <Stack align="center" gap="md">
          <Loader />
          <Text c="dimmed">Loading dashboard...</Text>
        </Stack>
      </Center>
    )
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError ? query.error.message : 'Unable to load dashboard.'

    return (
      <>
        <Alert color="red" title="Could not load dashboard">
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

  const dashboard = query.data
  if (!dashboard) return null

  if (dashboard.totalCompletedSessions === 0) {
    return <EmptyState />
  }

  return <Dashboard data={dashboard} />
}

function EmptyState() {
  return (
    <Card withBorder radius="md">
      <Stack gap="xs">
        <Text fw={600}>No completed sessions yet</Text>
        <Text c="dimmed">
          Complete your first interview to see progress metrics and analytics here.
        </Text>
        <Group pt="xs">
          <Button component={RouterLink} to="/interviews/new" variant="light">
            Start new interview
          </Button>
        </Group>
      </Stack>
    </Card>
  )
}

function Dashboard({ data }: { data: DashboardResponse }) {
  return (
    <Stack gap="md">
      <HeadlineStats data={data} />
      <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
        <ScoreTrendChart trend={data.scoreTrend} />
        <ScoreByFocusAreaChart data={data.scoresByFocusArea} />
      </SimpleGrid>
      <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
        <ScoreByInterviewTypeChart data={data.scoresByInterviewType} />
        <ScoreByDimensionChart data={data.scoresByDimension} />
      </SimpleGrid>
      <RecentSessionsTable sessions={data.recentSessions} />
    </Stack>
  )
}

function HeadlineStats({ data }: { data: DashboardResponse }) {
  return (
    <SimpleGrid cols={{ base: 2, xs: 2, md: 2 }} spacing="md">
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Text size="sm" c="dimmed" fw={500}>
            Total Completed
          </Text>
          <Text size="xl" fw={700}>
            {data.totalCompletedSessions}
          </Text>
        </Stack>
      </Card>
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Text size="sm" c="dimmed" fw={500}>
            Average Score
          </Text>
          <Text size="xl" fw={700}>
            {data.averageScore !== null ? `${data.averageScore}/100` : '—'}
          </Text>
        </Stack>
      </Card>
    </SimpleGrid>
  )
}

function ScoreTrendChart({ trend }: { trend: DashboardScoreTrendPointResponse[] }) {
  if (trend.length === 0) {
    return (
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Text size="sm" c="dimmed" fw={500}>
            Score Trend
          </Text>
          <Center h={200}>
            <Text c="dimmed">No trend data available</Text>
          </Center>
        </Stack>
      </Card>
    )
  }

  const chartData = trend.map((point) => ({
    date: new Date(point.completedAt).toLocaleDateString(),
    score: point.totalScore,
  }))

  return (
    <Card withBorder radius="md">
      <Stack gap="xs">
        <Text size="sm" c="dimmed" fw={500}>
          Score Trend
        </Text>
        <LineChart
          h={200}
          data={chartData}
          dataKey="date"
          series={[{ name: 'score', color: 'blue' }]}
          withDots
          withXAxis={false}
          withYAxis={false}
        />
      </Stack>
    </Card>
  )
}

function ScoreByFocusAreaChart(props: {
  data: { focusArea: string; score: number; sessionCount: number }[]
}) {
  if (props.data.length === 0) {
    return (
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Text size="sm" c="dimmed" fw={500}>
            Scores by Focus Area
          </Text>
          <Center h={200}>
            <Text c="dimmed">No data available</Text>
          </Center>
        </Stack>
      </Card>
    )
  }

  const chartData = props.data.map((item) => ({
    focusArea: item.focusArea,
    score: item.score,
  }))

  return (
    <Card withBorder radius="md">
      <Stack gap="xs">
        <Text size="sm" c="dimmed" fw={500}>
          Scores by Focus Area
        </Text>
        <BarChart
          h={200}
          data={chartData}
          dataKey="focusArea"
          series={[{ name: 'score', color: 'blue' }]}
          withXAxis={false}
          withYAxis={false}
        />
      </Stack>
    </Card>
  )
}

function ScoreByInterviewTypeChart(props: {
  data: { interviewType: string; score: number; sessionCount: number }[]
}) {
  if (props.data.length === 0) {
    return (
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Text size="sm" c="dimmed" fw={500}>
            Scores by Interview Type
          </Text>
          <Center h={200}>
            <Text c="dimmed">No data available</Text>
          </Center>
        </Stack>
      </Card>
    )
  }

  const chartData = props.data.map((item) => ({
    type: item.interviewType,
    score: item.score,
  }))

  return (
    <Card withBorder radius="md">
      <Stack gap="xs">
        <Text size="sm" c="dimmed" fw={500}>
          Scores by Interview Type
        </Text>
        <BarChart
          h={200}
          data={chartData}
          dataKey="type"
          series={[{ name: 'score', color: 'green' }]}
          withXAxis={false}
          withYAxis={false}
        />
      </Stack>
    </Card>
  )
}

function ScoreByDimensionChart(props: {
  data: { key: string; label: string; score: number; sampleCount: number }[]
}) {
  if (props.data.length === 0) {
    return (
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Text size="sm" c="dimmed" fw={500}>
            Scores by Rubric Dimension
          </Text>
          <Center h={200}>
            <Text c="dimmed">No data available</Text>
          </Center>
        </Stack>
      </Card>
    )
  }

  const chartData = props.data.map((item) => ({
    dimension: item.label,
    score: item.score,
  }))

  return (
    <Card withBorder radius="md">
      <Stack gap="xs">
        <Text size="sm" c="dimmed" fw={500}>
          Scores by Rubric Dimension
        </Text>
        <BarChart
          h={200}
          data={chartData}
          dataKey="dimension"
          series={[{ name: 'score', color: 'orange' }]}
          withXAxis={false}
          withYAxis={false}
        />
      </Stack>
    </Card>
  )
}

function RecentSessionsTable(props: {
  sessions: {
    id: string
    targetRole: string
    focusArea: string
    interviewType: string
    completedAt: string
    totalScore: number | null
  }[]
}) {
  if (props.sessions.length === 0) {
    return (
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Text size="sm" c="dimmed" fw={500}>
            Recent Sessions
          </Text>
          <Center h={100}>
            <Text c="dimmed">No recent sessions</Text>
          </Center>
        </Stack>
      </Card>
    )
  }

  return (
    <Card withBorder radius="md">
      <Stack gap="xs">
        <Text size="sm" c="dimmed" fw={500}>
          Recent Sessions
        </Text>
        <Table striped highlightOnHover withTableBorder>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Role</Table.Th>
              <Table.Th>Focus Area</Table.Th>
              <Table.Th>Type</Table.Th>
              <Table.Th>Score</Table.Th>
              <Table.Th>Date</Table.Th>
              <Table.Th>Action</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {props.sessions.map((session) => (
              <Table.Tr key={session.id}>
                <Table.Td>{session.targetRole}</Table.Td>
                <Table.Td>{session.focusArea}</Table.Td>
                <Table.Td>{session.interviewType}</Table.Td>
                <Table.Td>
                  <Badge color={scoreColor(session.totalScore)} variant="light">
                    {session.totalScore !== null ? `${session.totalScore}/100` : 'Pending'}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{new Date(session.completedAt).toLocaleDateString()}</Text>
                </Table.Td>
                <Table.Td>
                  <Button
                    component={RouterLink}
                    to={`/interviews/${session.id}`}
                    variant="subtle"
                    size="compact-sm"
                  >
                    View
                  </Button>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Stack>
    </Card>
  )
}

function scoreColor(score: number | null): string {
  if (score === null) return 'gray'
  if (score >= 80) return 'green'
  if (score >= 60) return 'yellow'
  return 'red'
}
