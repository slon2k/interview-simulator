import axios from 'axios'

export type ApiErrorBody = {
  error?: string
  message?: string
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
