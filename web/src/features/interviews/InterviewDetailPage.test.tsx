import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { forwardRef, type ComponentProps, type TextareaHTMLAttributes } from 'react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query'
import type { UseQueryResult } from '@tanstack/react-query'
import { MantineProvider } from '@mantine/core'
import { MemoryRouter } from 'react-router-dom'
import { ApiError } from '../../api/apiError'
import type { GetInterviewDetailsResponse, InterviewResponse } from '../../api/interviewApi'
import { InterviewDetailPage } from './InterviewDetailPage'
import * as interviewApi from '../../api/interviewApi'

vi.mock('@tanstack/react-query', async (importOriginal) => {
  const mod = await importOriginal<typeof import('@tanstack/react-query')>()
  return { ...mod, useQuery: vi.fn() }
})

// Mantine's Textarea autosize implementation accesses a ref before it is set in jsdom.
// Stub it with a plain labeled textarea that works with getByRole queries.
vi.mock('@mantine/core', async (importOriginal) => {
  const mod = await importOriginal<typeof import('@mantine/core')>()
  const TextareaStub = forwardRef<HTMLTextAreaElement, ComponentProps<typeof mod.Textarea>>(
    function Textarea({ label, minRows, autosize, error, ...props }, ref) {
      void autosize
      return (
        <label>
          {label}
          <textarea
            ref={ref}
            rows={minRows}
            {...(props as TextareaHTMLAttributes<HTMLTextAreaElement>)}
          />
          {error != null && (
            <span>{typeof error === 'string' ? error : JSON.stringify(error)}</span>
          )}
        </label>
      )
    }
  )
  return { ...mod, Textarea: TextareaStub }
})

vi.mock('react-router-dom', async (importOriginal) => {
  const mod = await importOriginal<typeof import('react-router-dom')>()
  return { ...mod, useParams: () => ({ interviewId: 'test-interview-id' }) }
})

vi.mock('../../api/interviewApi', () => ({
  getInterview: vi.fn(),
  getInterviewDetails: vi.fn(),
  startInterview: vi.fn(),
  submitAnswer: vi.fn(),
  completeInterview: vi.fn(),
}))

function mockQuery(overrides: Partial<UseQueryResult<InterviewResponse>>) {
  vi.mocked(useQuery).mockReturnValue({
    isLoading: false,
    isError: false,
    data: undefined,
    error: null,
    refetch: vi.fn(),
    ...overrides,
  } as unknown as UseQueryResult<InterviewResponse>)
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return render(<InterviewDetailPage />, {
    wrapper: ({ children }) => (
      <QueryClientProvider client={queryClient}>
        <MantineProvider>
          <MemoryRouter>{children}</MemoryRouter>
        </MantineProvider>
      </QueryClientProvider>
    ),
  })
}

const baseInterview: InterviewResponse = {
  id: 'test-interview-id',
  userId: 'github|1',
  targetRole: 'Backend Engineer',
  focusArea: 'dotnet',
  interviewType: 'Technical',
  seniorityLevel: 'Senior',
  questionCount: 5,
  answeredCount: 0,
  status: 'Created',
  createdAt: '2026-01-01T00:00:00Z',
  startedAt: null,
  completedAt: null,
  totalScore: null,
  currentQuestion: null,
}

const activeInterview: InterviewResponse = {
  ...baseInterview,
  status: 'Active',
  answeredCount: 1,
  startedAt: '2026-01-01T00:01:00Z',
  currentQuestion: { text: 'Explain async/await in C#', topic: 'Async Programming', turnNumber: 2 },
}

const completedInterview: InterviewResponse = {
  ...baseInterview,
  status: 'Completed',
  answeredCount: 5,
  startedAt: '2026-01-01T00:01:00Z',
  completedAt: '2026-01-01T00:30:00Z',
}

const completedDetails: GetInterviewDetailsResponse = {
  ...completedInterview,
  summary: { text: 'A strong interview.', createdAt: '2026-01-01T00:31:00Z' },
  turns: [
    {
      turnNumber: 1,
      question: { text: 'Explain dependency injection.', topic: 'Architecture' },
      answer: { text: 'It separates construction from use.', createdAt: '2026-01-01T00:10:00Z' },
      evaluation: {
        overallScore: 84,
        overallFeedback: 'Clear explanation with a useful example.',
        dimensions: [
          {
            key: 'clarity',
            label: 'Clarity',
            score: 88,
            feedback: 'The answer was concise and easy to follow.',
          },
        ],
      },
      createdAt: '2026-01-01T00:05:00Z',
    },
  ],
}

function mockCompletedQueries(details: Partial<UseQueryResult<GetInterviewDetailsResponse>> = {}) {
  vi.mocked(useQuery).mockReset()
  vi.mocked(useQuery).mockReturnValueOnce({
    isLoading: false,
    isError: false,
    data: completedInterview,
    error: null,
    refetch: vi.fn(),
  } as unknown as UseQueryResult<InterviewResponse>)
  vi.mocked(useQuery).mockReturnValueOnce({
    isLoading: false,
    isError: false,
    data: { ...completedDetails, ...details.data },
    error: null,
    refetch: vi.fn(),
    ...details,
  } as unknown as UseQueryResult<GetInterviewDetailsResponse>)
}

describe('InterviewDetailPage', () => {
  beforeEach(() => {
    vi.mocked(useQuery).mockReset()
    vi.mocked(interviewApi.getInterviewDetails).mockReset()
    vi.mocked(interviewApi.startInterview).mockReset()
    vi.mocked(interviewApi.submitAnswer).mockReset()
    vi.mocked(interviewApi.completeInterview).mockReset()
  })

  it('shows a loading indicator while fetching', () => {
    mockQuery({ isLoading: true })
    renderPage()
    expect(screen.getByText(/loading interview/i)).toBeInTheDocument()
  })

  it('shows an error alert when the query fails', () => {
    mockQuery({ isError: true, error: new ApiError(500, 'Network failure') })
    renderPage()
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Network failure')).toBeInTheDocument()
  })

  it('shows a retry button that calls refetch', async () => {
    const refetch = vi.fn()
    mockQuery({ isError: true, error: new ApiError(500, 'Oops'), refetch })
    renderPage()

    await userEvent.click(screen.getByRole('button', { name: /retry/i }))

    expect(refetch).toHaveBeenCalledOnce()
  })

  describe('created state', () => {
    it('renders interview metadata', () => {
      mockQuery({ data: baseInterview })
      renderPage()
      expect(screen.getByText('Backend Engineer')).toBeInTheDocument()
      expect(screen.getByText('dotnet')).toBeInTheDocument()
      expect(screen.getByText('Technical')).toBeInTheDocument()
      expect(screen.getByText('Senior')).toBeInTheDocument()
    })

    it('shows a start interview button', () => {
      mockQuery({ data: baseInterview })
      renderPage()
      expect(screen.getByRole('button', { name: /start interview/i })).toBeInTheDocument()
    })

    it('calls startInterview when the button is clicked', async () => {
      vi.mocked(interviewApi.startInterview).mockResolvedValue({} as never)
      mockQuery({ data: baseInterview })
      renderPage()

      await userEvent.click(screen.getByRole('button', { name: /start interview/i }))

      expect(interviewApi.startInterview).toHaveBeenCalledWith('test-interview-id')
    })

    it('shows an error alert when starting fails', async () => {
      vi.mocked(interviewApi.startInterview).mockRejectedValue(new ApiError(409, 'Already started'))
      mockQuery({ data: baseInterview })
      renderPage()

      await userEvent.click(screen.getByRole('button', { name: /start interview/i }))

      await waitFor(() => expect(screen.getByText('Already started')).toBeInTheDocument())
    })
  })

  describe('active state', () => {
    it('renders the current question', () => {
      mockQuery({ data: activeInterview })
      renderPage()
      expect(screen.getByText('Explain async/await in C#')).toBeInTheDocument()
      expect(screen.getByText(/async programming/i)).toBeInTheDocument()
    })

    it('renders the answer textarea', () => {
      mockQuery({ data: activeInterview })
      renderPage()
      expect(screen.getByRole('textbox', { name: /your answer/i })).toBeInTheDocument()
    })

    it('shows a stop interview button', () => {
      mockQuery({ data: activeInterview })
      renderPage()
      expect(screen.getByRole('button', { name: /stop interview/i })).toBeInTheDocument()
    })

    it('calls submitAnswer with the typed answer', async () => {
      vi.mocked(interviewApi.submitAnswer).mockResolvedValue({} as never)
      mockQuery({ data: activeInterview })
      renderPage()

      await userEvent.type(screen.getByRole('textbox', { name: /your answer/i }), 'My answer')
      await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))

      expect(interviewApi.submitAnswer).toHaveBeenCalledWith(
        'test-interview-id',
        expect.objectContaining({ answer: 'My answer', turnNumber: 2 })
      )
    })

    it('shows a validation error when submitting an empty answer', async () => {
      mockQuery({ data: activeInterview })
      renderPage()

      await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))

      expect(screen.getByText(/answer is required/i)).toBeInTheDocument()
      expect(interviewApi.submitAnswer).not.toHaveBeenCalled()
    })

    it('opens the stop confirmation modal', async () => {
      mockQuery({ data: activeInterview })
      renderPage()

      await userEvent.click(screen.getByRole('button', { name: /stop interview/i }))

      // Mantine's Modal opens with a transition/portal, so the body isn't in the DOM
      // on the same tick — wait for it rather than querying synchronously.
      expect(await screen.findByText(/your progress so far will be saved/i)).toBeInTheDocument()
    })

    it('calls completeInterview when the modal is confirmed', async () => {
      vi.mocked(interviewApi.completeInterview).mockResolvedValue(undefined)
      mockQuery({ data: activeInterview })
      renderPage()

      await userEvent.click(screen.getByRole('button', { name: /stop interview/i }))
      await screen.findByRole('button', { name: /cancel/i })
      // The modal Portal renders before the main form in the DOM, so [0] is the confirm button
      await userEvent.click(screen.getAllByRole('button', { name: /stop interview/i })[0]!)

      expect(interviewApi.completeInterview).toHaveBeenCalledWith('test-interview-id')
    })
  })

  describe('completed state', () => {
    it('shows the completion message', () => {
      mockCompletedQueries()
      renderPage()
      expect(screen.getByText(/interview completed/i)).toBeInTheDocument()
    })

    it('shows answered vs total count', () => {
      mockCompletedQueries()
      renderPage()
      expect(screen.getByText(/answered 5 of 5/i)).toBeInTheDocument()
    })

    it('does not show the answer form', () => {
      mockCompletedQueries()
      renderPage()
      expect(screen.queryByRole('textbox', { name: /your answer/i })).not.toBeInTheDocument()
    })

    it('renders the summary and expandable turn evaluation', async () => {
      mockCompletedQueries()
      renderPage()

      expect(screen.getByText('A strong interview.')).toBeInTheDocument()
      expect(screen.getByText(/question 1: explain dependency injection/i)).toBeInTheDocument()
      expect(screen.getByText('84/100')).toBeInTheDocument()
      const turnControl = screen.getByRole('button', { name: /question 1/i })
      expect(turnControl).toHaveAttribute('aria-expanded', 'false')

      await userEvent.click(turnControl)

      expect(screen.getByText('It separates construction from use.')).toBeInTheDocument()
      expect(screen.getByText('Clarity')).toBeInTheDocument()
      expect(screen.getByText('The answer was concise and easy to follow.')).toBeInTheDocument()
      expect(turnControl).toHaveAttribute('aria-expanded', 'true')
    })

    it('shows a pending summary while keeping turns visible', () => {
      mockCompletedQueries({ data: { ...completedDetails, summary: null } })
      renderPage()

      expect(screen.getByText('Summary pending...')).toBeInTheDocument()
      expect(screen.getByText(/question 1: explain dependency injection/i)).toBeInTheDocument()
    })

    it('enables the details query only for completed interviews', () => {
      mockCompletedQueries()
      renderPage()

      expect(vi.mocked(useQuery).mock.calls[1]?.[0]).toEqual(
        expect.objectContaining({
          queryKey: ['interview-details', 'test-interview-id'],
          enabled: true,
        })
      )
    })

    it('shows a loading state while feedback is loading', () => {
      mockCompletedQueries({ isLoading: true })
      renderPage()

      expect(screen.getByText(/loading interview feedback/i)).toBeInTheDocument()
    })

    it('shows a feedback error and retry button when details loading fails', async () => {
      const refetch = vi.fn()
      mockCompletedQueries({
        isError: true,
        error: new ApiError(500, 'Feedback unavailable'),
        refetch,
      })
      renderPage()

      expect(screen.getByText('Feedback unavailable')).toBeInTheDocument()
      await userEvent.click(screen.getByRole('button', { name: /retry/i }))
      expect(refetch).toHaveBeenCalledOnce()
    })
  })

  describe('details query isolation', () => {
    it.each([
      ['Created', baseInterview],
      ['Active', activeInterview],
    ] as const)('does not create a details query for %s interviews', (_status, interview) => {
      mockQuery({ data: interview })
      renderPage()

      expect(interviewApi.getInterviewDetails).not.toHaveBeenCalled()
      expect(vi.mocked(useQuery)).toHaveBeenCalledOnce()
    })
  })
})
