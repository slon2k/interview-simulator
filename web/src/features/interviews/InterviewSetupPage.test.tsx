import { fireEvent, screen, waitFor, cleanup } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { InterviewSetupPage } from './InterviewSetupPage'
import { renderWithProviders } from '../../test/renderWithProviders'
import { ApiError } from '../../api/apiError'
import * as interviewApi from '../../api/interviewApi'

vi.mock('../../api/interviewApi')

const mockNavigate = vi.hoisted(() => vi.fn())

vi.mock('react-router-dom', async (importOriginal) => {
  const mod = await importOriginal<typeof import('react-router-dom')>()
  return { ...mod, useNavigate: () => mockNavigate }
})

// Selects an option from a Mantine Select identified by its exact label:
// click to open the dropdown, then click the option.
function selectOption(label: string, option: string) {
  fireEvent.click(screen.getByLabelText(label, { exact: false, selector: 'input' }))
  fireEvent.click(screen.getByRole('option', { name: option }))
}

// Fills every required field so the form passes client validation.
function fillValidForm() {
  // Role is a plain TextInput (with a datalist), driven via change.
  fireEvent.change(screen.getByLabelText('Role', { exact: false, selector: 'input' }), {
    target: { value: 'Backend Engineer' },
  })
  selectOption('Topic', 'dotnet')
  selectOption('Interview type', 'Technical')
  selectOption('Seniority', 'Senior')
}

describe('InterviewSetupPage', () => {
  beforeEach(() => {
    mockNavigate.mockClear()
    vi.mocked(interviewApi.createInterview).mockReset()
  })

  afterEach(() => {
    cleanup()
  })

  it('renders the setup form', () => {
    renderWithProviders(<InterviewSetupPage />)
    expect(screen.getByRole('heading', { name: 'New Interview' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /start interview/i })).toBeInTheDocument()
  })

  it('shows client validation errors and does not call the API when submitted empty', async () => {
    const { container } = renderWithProviders(<InterviewSetupPage />)
    fireEvent.submit(container.querySelector('form')!)

    expect(await screen.findByText('Role is required.')).toBeInTheDocument()
    expect(await screen.findByText('Topic is required.')).toBeInTheDocument()
    expect(await screen.findByText('Interview type is required.')).toBeInTheDocument()
    expect(await screen.findByText('Seniority is required.')).toBeInTheDocument()
    expect(interviewApi.createInterview).not.toHaveBeenCalled()
  })

  it('calls createInterview and navigates on successful submit', async () => {
    const created = { id: 'abc-123' }
    vi.mocked(interviewApi.createInterview).mockResolvedValue(
      created as unknown as Awaited<ReturnType<typeof interviewApi.createInterview>>
    )

    const { container } = renderWithProviders(<InterviewSetupPage />)
    fillValidForm()
    fireEvent.submit(container.querySelector('form')!)

    await waitFor(() => expect(interviewApi.createInterview).toHaveBeenCalled())
    expect(mockNavigate).toHaveBeenCalledWith('/interviews/abc-123')
  })

  it('shows a form-level alert when the API fails without field errors', async () => {
    vi.mocked(interviewApi.createInterview).mockRejectedValue(
      new ApiError(500, 'Server exploded', { detail: 'Server exploded' })
    )

    const { container } = renderWithProviders(<InterviewSetupPage />)
    fillValidForm()
    fireEvent.submit(container.querySelector('form')!)

    expect(await screen.findByRole('alert')).toHaveTextContent('Server exploded')
  })

  it('maps server-side validation errors onto their fields', async () => {
    vi.mocked(interviewApi.createInterview).mockRejectedValue(
      new ApiError(400, 'Validation failed', {
        errors: { TargetRole: ['Target role is not allowed.'] },
      })
    )

    const { container } = renderWithProviders(<InterviewSetupPage />)
    fillValidForm()
    fireEvent.submit(container.querySelector('form')!)

    // The server error lands on the field, not in a form-level alert.
    expect(await screen.findByText('Target role is not allowed.')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
