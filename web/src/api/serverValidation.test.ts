import { describe, it, expect } from 'vitest'
import { ApiError } from './apiError'
import { extractFieldErrors, toFormFieldName } from './serverValidation'

describe('toFormFieldName', () => {
  it('lower-cases the first character of PascalCase server fields', () => {
    expect(toFormFieldName('TargetRole')).toBe('targetRole')
    expect(toFormFieldName('QuestionCount')).toBe('questionCount')
  })

  it('leaves already-camelCase fields unchanged', () => {
    expect(toFormFieldName('focusArea')).toBe('focusArea')
  })

  it('handles empty strings', () => {
    expect(toFormFieldName('')).toBe('')
  })
})

describe('extractFieldErrors', () => {
  it('maps PascalCase ProblemDetails errors onto camelCase form fields', () => {
    const error = new ApiError(400, 'Validation failed', {
      errors: {
        TargetRole: ['Target role is required.'],
        QuestionCount: ['Question count must be greater than zero.'],
      },
    })

    expect(extractFieldErrors(error)).toEqual({
      targetRole: 'Target role is required.',
      questionCount: 'Question count must be greater than zero.',
    })
  })

  it('joins multiple messages for a single field', () => {
    const error = new ApiError(400, 'Validation failed', {
      errors: { InterviewType: ['Interview type is required.', 'Invalid interview type: x'] },
    })

    expect(extractFieldErrors(error).interviewType).toBe(
      'Interview type is required., Invalid interview type: x'
    )
  })

  it('returns an empty object for a non-validation ApiError', () => {
    const error = new ApiError(500, 'Server error', { detail: 'boom' })
    expect(extractFieldErrors(error)).toEqual({})
  })

  it('returns an empty object for non-ApiError input', () => {
    expect(extractFieldErrors(new Error('nope'))).toEqual({})
    expect(extractFieldErrors(null)).toEqual({})
  })
})
