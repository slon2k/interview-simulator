import { ApiError, type ApiErrorBody } from './apiError'

// Lower-cases the first character so server field names emitted by FluentValidation
export function toFormFieldName(serverField: string): string {
  const first = serverField.charAt(0)
  if (first === '') {
    return serverField
  }

  return first.toLowerCase() + serverField.slice(1)
}

// Extracts field-level validation errors from an ApiError produced by a 400
// ProblemDetails response, keyed by camelCase form field name.
export function extractFieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError)) {
    return {}
  }

  const body = error.responseBody
  if (typeof body !== 'object' || body === null) {
    return {}
  }

  const errors = (body as ApiErrorBody).errors
  if (!errors || typeof errors !== 'object') {
    return {}
  }

  const result: Record<string, string> = {}
  for (const [field, messages] of Object.entries(errors)) {
    if (Array.isArray(messages) && messages.length > 0) {
      result[toFormFieldName(field)] = messages.join(', ')
    }
  }

  return result
}
