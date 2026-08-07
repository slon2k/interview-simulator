import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useQuery } from '@tanstack/react-query'
import { renderWithProviders } from '../../test/renderWithProviders'
import type { UseQueryResult } from '@tanstack/react-query'
import { ApiError } from '../../api/apiError'
import type { InterviewSummary } from '../../api/interviewApi'
import { InterviewListPage } from './InterviewListPage'
import { formatProgress, statusColor } from './interviewListHelpers'

vi.mock('@tanstack/react-query', async (importOriginal) => {
  const mod = await importOriginal<typeof import('@tanstack/react-query')>()
  return { ...mod, useQuery: vi.fn() }
})

function mockQuery(overrides: Partial<UseQueryResult<InterviewSummary[]>>) {
  vi.mocked(useQuery).mockReturnValue({
    isLoading: false,
    isError: false,
    data: undefined,
    error: null,
    refetch: vi.fn(),
    ...overrides,
  } as unknown as UseQueryResult<InterviewSummary[]>)
}

const fakeInterview: InterviewSummary = {
  id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  userId: 'github|1',
  targetRole: 'Backend Engineer',
  focusArea: 'dotnet',
  interviewType: 'Technical',
  seniorityLevel: 'Senior',
  questionCount: 5,
  answeredCount: 3,
  status: 'Active',
  createdAt: '2026-01-01T00:00:00Z',
  startedAt: null,
  completedAt: null,
  totalScore: null,
}

function renderPage() {
  return renderWithProviders(<InterviewListPage />)
}

describe('formatProgress', () => {
  it('formats answered/total', () => {
    expect(formatProgress({ ...fakeInterview, answeredCount: 3, questionCount: 5 })).toBe('3/5')
  })

  it('returns 0/0 when questionCount is 0', () => {
    expect(formatProgress({ ...fakeInterview, answeredCount: 0, questionCount: 0 })).toBe('0/0')
  })
})

describe('statusColor', () => {
  it('returns blue for Active', () => {
    expect(statusColor('Active')).toBe('blue')
  })

  it('returns green for Completed', () => {
    expect(statusColor('Completed')).toBe('green')
  })

  it('returns gray for Created', () => {
    expect(statusColor('Created')).toBe('gray')
  })
})

describe('InterviewListPage', () => {
  beforeEach(() => {
    vi.mocked(useQuery).mockReset()
  })

  it('shows a loading indicator while fetching', () => {
    mockQuery({ isLoading: true })
    renderPage()
    expect(screen.getByText(/loading interviews/i)).toBeInTheDocument()
  })

  it('shows an error alert when the query fails', () => {
    mockQuery({ isError: true, error: new ApiError(500, 'Network failure') })
    renderPage()
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Network failure')).toBeInTheDocument()
  })

  it('shows a retry button on error', async () => {
    const refetch = vi.fn()
    mockQuery({ isError: true, error: new ApiError(500, 'Oops'), refetch })
    renderPage()

    await userEvent.click(screen.getByRole('button', { name: /retry/i }))

    expect(refetch).toHaveBeenCalledOnce()
  })

  it('shows an empty-state message when there are no interviews', () => {
    mockQuery({ data: [] })
    renderPage()
    expect(screen.getByText(/no interviews yet/i)).toBeInTheDocument()
  })

  it('renders a row for each interview', () => {
    mockQuery({ data: [fakeInterview] })
    renderPage()
    expect(screen.getByText('Backend Engineer')).toBeInTheDocument()
    expect(screen.getByText('dotnet')).toBeInTheDocument()
    expect(screen.getByText('3/5')).toBeInTheDocument()
  })

  it('shows Continue action for active interviews', () => {
    mockQuery({ data: [{ ...fakeInterview, status: 'Active' }] })
    renderPage()
    expect(screen.getByRole('link', { name: /continue/i })).toBeInTheDocument()
  })

  it('shows View action for completed interviews', () => {
    mockQuery({ data: [{ ...fakeInterview, status: 'Completed' }] })
    renderPage()
    expect(screen.getByRole('link', { name: 'View' })).toBeInTheDocument()
  })
})
