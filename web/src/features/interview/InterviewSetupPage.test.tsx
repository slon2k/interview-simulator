import { fireEvent, cleanup } from '@testing-library/react'
import { screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { InterviewSetupPage } from './InterviewSetupPage'
import { validate } from './validateInterviewSetup'
import { renderWithProviders } from '../../test/renderWithProviders'
import * as interviewApi from '../../api/interviewApi'

vi.mock('../../api/interviewApi')
vi.mock('./validateInterviewSetup')

const mockNavigate = vi.hoisted(() => vi.fn())

vi.mock('react-router-dom', async (importOriginal) => {
  const mod = await importOriginal<typeof import('react-router-dom')>()
  return { ...mod, useNavigate: () => mockNavigate }
})

describe('InterviewSetupPage', () => {
  afterEach(() => {
    cleanup()
  })

  beforeEach(() => {
    mockNavigate.mockClear()
    vi.mocked(validate).mockReset()
  })

  it('renders the setup form', () => {
    vi.mocked(validate).mockReturnValue({})
    renderWithProviders(<InterviewSetupPage />)
    expect(screen.getByRole('heading', { name: 'New Interview' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /start interview/i })).toBeInTheDocument()
  })

  it('shows validation errors when submitted empty', async () => {
    vi.mocked(validate).mockReturnValue({
      targetRole: 'Role is required.',
      focusArea: 'Topic is required.',
      interviewType: 'Interview type is required.',
      seniorityLevel: 'Seniority is required.',
      questionCount: 'Question count must be greater than zero.',
    })

    const { container } = renderWithProviders(<InterviewSetupPage />)
    fireEvent.submit(container.querySelector('form')!)

    expect(await screen.findByText('Role is required.')).toBeInTheDocument()
    expect(await screen.findByText('Topic is required.')).toBeInTheDocument()
    expect(await screen.findByText('Interview type is required.')).toBeInTheDocument()
    expect(await screen.findByText('Seniority is required.')).toBeInTheDocument()
    expect(await screen.findByText('Question count must be greater than zero.')).toBeInTheDocument()
  })

  it('calls createInterview and navigates on successful submit', async () => {
    vi.mocked(validate).mockReturnValue({})
    const created = { id: 'abc-123' }
    vi.mocked(interviewApi.createInterview).mockResolvedValue(
      created as unknown as Awaited<ReturnType<typeof interviewApi.createInterview>>
    )

    const { container } = renderWithProviders(<InterviewSetupPage />)
    fireEvent.submit(container.querySelector('form')!)

    await vi.waitFor(() => {
      expect(interviewApi.createInterview).toHaveBeenCalled()
    })
    expect(mockNavigate).toHaveBeenCalledWith('/interviews/abc-123')
  })

  it('shows an API error alert when submission fails', async () => {
    vi.mocked(validate).mockReturnValue({})
    vi.mocked(interviewApi.createInterview).mockRejectedValue(new Error('Server error'))

    const { container } = renderWithProviders(<InterviewSetupPage />)
    fireEvent.submit(container.querySelector('form')!)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
  })
})

