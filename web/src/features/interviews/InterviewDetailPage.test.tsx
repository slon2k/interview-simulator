import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { forwardRef, type ComponentProps, type TextareaHTMLAttributes } from 'react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query'
import type { UseQueryResult } from '@tanstack/react-query'
import { MantineProvider } from '@mantine/core'
import { MemoryRouter } from 'react-router-dom'
import { ApiError } from '../../api/apiError'
import type { GetInterviewResponse } from '../../api/interviewApi'
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
  startInterview: vi.fn(),
  submitAnswer: vi.fn(),
  completeInterview: vi.fn(),
}))

function mockQuery(overrides: Partial<UseQueryResult<GetInterviewResponse>>) {
  vi.mocked(useQuery).mockReturnValue({
    isLoading: false,
    isError: false,
    data: undefined,
    error: null,
    refetch: vi.fn(),
    ...overrides,
  } as unknown as UseQueryResult<GetInterviewResponse>)
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

const baseInterview: GetInterviewResponse = {
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
  feedback: null,
  currentQuestion: null,
}

const activeInterview: GetInterviewResponse = {
  ...baseInterview,
  status: 'Active',
  answeredCount: 1,
  startedAt: '2026-01-01T00:01:00Z',
  currentQuestion: { text: 'Explain async/await in C#', topic: 'Async Programming' },
}

const completedInterview: GetInterviewResponse = {
  ...baseInterview,
  status: 'Completed',
  answeredCount: 5,
  startedAt: '2026-01-01T00:01:00Z',
  completedAt: '2026-01-01T00:30:00Z',
}

describe('InterviewDetailPage', () => {
  beforeEach(() => {
    vi.mocked(useQuery).mockReset()
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
      mockQuery({ data: completedInterview })
      renderPage()
      expect(screen.getByText(/interview completed/i)).toBeInTheDocument()
    })

    it('shows answered vs total count', () => {
      mockQuery({ data: completedInterview })
      renderPage()
      expect(screen.getByText(/answered 5 of 5/i)).toBeInTheDocument()
    })

    it('does not show the answer form', () => {
      mockQuery({ data: completedInterview })
      renderPage()
      expect(screen.queryByRole('textbox', { name: /your answer/i })).not.toBeInTheDocument()
    })
  })
})
