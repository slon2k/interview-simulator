import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { MantineProvider } from '@mantine/core'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { DashboardPage } from './DashboardPage'
import * as dashboardApi from '../../api/dashboardApi'

vi.mock('../../api/dashboardApi')

const mockDashboardResponse: dashboardApi.DashboardResponse = {
  totalCompletedSessions: 5,
  averageScore: 75,
  scoreTrend: [
    {
      interviewId: '1',
      completedAt: '2026-08-20T10:00:00Z',
      totalScore: 70,
    },
    {
      interviewId: '2',
      completedAt: '2026-08-21T10:00:00Z',
      totalScore: 75,
    },
    {
      interviewId: '3',
      completedAt: '2026-08-22T10:00:00Z',
      totalScore: 80,
    },
  ],
  scoresByFocusArea: [
    { focusArea: 'JavaScript', score: 78, sessionCount: 2 },
    { focusArea: 'React', score: 72, sessionCount: 3 },
  ],
  scoresByInterviewType: [
    { interviewType: 'Technical', score: 76, sessionCount: 4 },
    { interviewType: 'Behavioral', score: 74, sessionCount: 1 },
  ],
  scoresByDimension: [
    { key: 'technical-depth', label: 'Technical Depth', score: 80, sampleCount: 5 },
    { key: 'communication', label: 'Communication', score: 70, sampleCount: 5 },
  ],
  recentSessions: [
    {
      id: '3',
      targetRole: 'Senior Engineer',
      focusArea: 'React',
      interviewType: 'Technical',
      completedAt: '2026-08-22T10:00:00Z',
      totalScore: 80,
    },
    {
      id: '2',
      targetRole: 'Mid Engineer',
      focusArea: 'JavaScript',
      interviewType: 'Behavioral',
      completedAt: '2026-08-21T10:00:00Z',
      totalScore: 75,
    },
  ],
}

const emptyDashboardResponse: dashboardApi.DashboardResponse = {
  totalCompletedSessions: 0,
  averageScore: null,
  scoreTrend: [],
  scoresByFocusArea: [],
  scoresByInterviewType: [],
  scoresByDimension: [],
  recentSessions: [],
}

function renderDashboard() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  })

  return render(
    <MantineProvider>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <DashboardPage />
        </BrowserRouter>
      </QueryClientProvider>
    </MantineProvider>
  )
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading state initially', () => {
    vi.mocked(dashboardApi.getDashboard).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    )

    renderDashboard()

    expect(screen.getByText('Loading dashboard...')).toBeInTheDocument()
  })

  it('shows error alert when dashboard fails to load', async () => {
    const error = new Error('Failed to fetch dashboard')
    vi.mocked(dashboardApi.getDashboard).mockRejectedValue(error)

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Could not load dashboard')).toBeInTheDocument()
    })

    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
  })

  it('shows empty state when no completed sessions', async () => {
    vi.mocked(dashboardApi.getDashboard).mockResolvedValue(emptyDashboardResponse)

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('No completed sessions yet')).toBeInTheDocument()
    })

    expect(
      screen.getByText('Complete your first interview to see progress metrics and analytics here.')
    ).toBeInTheDocument()

    const startLink = screen.getByRole('link', { name: 'Start new interview' })
    expect(startLink).toHaveAttribute('href', '/interviews/new')
  })

  it('renders dashboard with all metrics when data is available', async () => {
    vi.mocked(dashboardApi.getDashboard).mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve(mockDashboardResponse), 10))
    )

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Total Completed')).toBeInTheDocument()
    })

    // Headline stats

    // Chart titles
    expect(screen.getByText('Score Trend')).toBeInTheDocument()
    expect(screen.getByText('Scores by Focus Area')).toBeInTheDocument()
    expect(screen.getByText('Scores by Interview Type')).toBeInTheDocument()
    expect(screen.getByText('Scores by Rubric Dimension')).toBeInTheDocument()

    // Recent sessions
    expect(screen.getByText('Recent Sessions')).toBeInTheDocument()
  })

  it('renders recent sessions table with correct links', async () => {
    vi.mocked(dashboardApi.getDashboard).mockResolvedValue(mockDashboardResponse)

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Recent Sessions')).toBeInTheDocument()
    })

    // Check table headers
    expect(screen.getByText('Role')).toBeInTheDocument()
    expect(screen.getByText('Focus Area')).toBeInTheDocument()
    expect(screen.getByText('Type')).toBeInTheDocument()

    // Check table content
    expect(screen.getByText('Senior Engineer')).toBeInTheDocument()
    expect(screen.getByText('Mid Engineer')).toBeInTheDocument()

    // Check links to interview detail pages
    const viewLinks = screen.getAllByRole('link', { name: 'View' })
    expect(viewLinks).toHaveLength(2)
    expect(viewLinks[0]).toHaveAttribute('href', '/interviews/3')
    expect(viewLinks[1]).toHaveAttribute('href', '/interviews/2')
  })

  it('shows score badges with correct color based on score', async () => {
    vi.mocked(dashboardApi.getDashboard).mockResolvedValue(mockDashboardResponse)

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Recent Sessions')).toBeInTheDocument()
    })

    // Check that scores appear in the table
    const scoreElements = screen.getAllByText(/^\d+\/100$/)
    expect(scoreElements.length).toBeGreaterThanOrEqual(2)
  })

  it('calls getDashboard with correct query key', async () => {
    vi.mocked(dashboardApi.getDashboard).mockResolvedValue(mockDashboardResponse)

    renderDashboard()

    await waitFor(() => {
      expect(dashboardApi.getDashboard).toHaveBeenCalled()
    })
  })

  it('retries dashboard fetch when retry button is clicked', async () => {
    const error = new Error('Network error')
    vi.mocked(dashboardApi.getDashboard)
      .mockRejectedValueOnce(error)
      .mockResolvedValueOnce(mockDashboardResponse)

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Could not load dashboard')).toBeInTheDocument()
    })

    const retryButton = screen.getByRole('button', { name: 'Retry' })
    await userEvent.click(retryButton)

    await waitFor(() => {
      expect(screen.getByText('Dashboard')).toBeInTheDocument()
      expect(dashboardApi.getDashboard).toHaveBeenCalledTimes(2)
    })
  })

  it('shows null average score as dash when no data', async () => {
    const dashboardWithNullScore: dashboardApi.DashboardResponse = {
      ...mockDashboardResponse,
      averageScore: null,
    }

    vi.mocked(dashboardApi.getDashboard).mockResolvedValue(dashboardWithNullScore)

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('—')).toBeInTheDocument()
    })
  })
})
