import { describe, it, expect } from 'vitest'
import { validate } from './validateInterviewSetup'
import type { SetupFormValues } from './validateInterviewSetup'

const validValues: SetupFormValues = {
  targetRole: 'Backend Engineer',
  focusArea: 'dotnet',
  interviewType: 'Technical',
  seniorityLevel: 'Senior',
  questionCount: 5,
}

describe('validate', () => {
  it('returns an error for each empty field', () => {
    const errors = validate({
      targetRole: '',
      focusArea: '',
      interviewType: '',
      seniorityLevel: '',
      questionCount: 0,
    })

    expect(errors.targetRole).toBe('Role is required.')
    expect(errors.focusArea).toBe('Topic is required.')
    expect(errors.interviewType).toBe('Interview type is required.')
    expect(errors.seniorityLevel).toBe('Seniority is required.')
    expect(errors.questionCount).toBe('Question count must be greater than zero.')
  })

  it('returns a questionCount error when count is zero', () => {
    const errors = validate({ ...validValues, questionCount: 0 })
    expect(errors.questionCount).toBe('Question count must be greater than zero.')
  })

  it('returns a questionCount error when count is negative', () => {
    const errors = validate({ ...validValues, questionCount: -1 })
    expect(errors.questionCount).toBe('Question count must be greater than zero.')
  })

  it('returns no errors for valid input', () => {
    const errors = validate(validValues)
    expect(Object.keys(errors)).toHaveLength(0)
  })

  it('returns a role error for whitespace-only role', () => {
    // whitespace is caught by .trim()
    const errors = validate({ ...validValues, targetRole: '   ' })
    expect(errors.targetRole).toBe('Role is required.')
  })
})
