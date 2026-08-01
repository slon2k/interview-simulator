import axios from 'axios'

export type ApiErrorBody = {
  type?: string
  title?: string
  status?: number
  detail?: string
  error?: string
  message?: string
  errors?: Record<string, string[]>
  traceId?: string
}

export class ApiError extends Error {
  public readonly status: number
  public readonly responseBody: unknown

  constructor(status: number, message: string, responseBody?: unknown) {
    super(message)
    this.status = status
    this.name = 'ApiError'
    if (responseBody) {
      this.responseBody = responseBody
    }
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error
  }

  if (axios.isAxiosError(error)) {
    const status = error.response?.status ?? 0
    const responseBody: unknown = error.response?.data

    const message =
      getErrorMessage(responseBody) ?? error.message ?? `Request failed with status ${status}`

    return new ApiError(status, message, responseBody)
  }

  if (error instanceof Error) {
    return new ApiError(0, error.message)
  }

  return new ApiError(0, 'Unknown API error', error)
}

function getErrorMessage(responseBody: unknown): string | null {
  if (isApiErrorBody(responseBody)) {
    const validationMessage = formatValidationErrors(responseBody)
    if (validationMessage) {
      return validationMessage
    }

    if (typeof responseBody.message === 'string' && responseBody.message.length > 0) {
      return responseBody.message
    }

    if (typeof responseBody.detail === 'string' && responseBody.detail.length > 0) {
      return responseBody.detail
    }

    if (typeof responseBody.error === 'string' && responseBody.error.length > 0) {
      return responseBody.error
    }

    if (typeof responseBody.title === 'string' && responseBody.title.length > 0) {
      return responseBody.title
    }
  }

  if (
    typeof responseBody === 'object' &&
    responseBody !== null &&
    'message' in responseBody &&
    typeof responseBody.message === 'string'
  ) {
    return responseBody.message
  }

  return null
}

function formatValidationErrors(responseBody: ApiErrorBody): string | null {
  if (!responseBody.errors || typeof responseBody.errors !== 'object') {
    return null
  }

  const entries = Object.entries(responseBody.errors)
    .filter(([, messages]) => Array.isArray(messages) && messages.length > 0)
    .map(([field, messages]) => `${field}: ${messages.join(', ')}`)

  if (entries.length === 0) {
    return null
  }

  return `Validation failed: ${entries.join('; ')}`
}

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  return typeof value === 'object' && value !== null
}
