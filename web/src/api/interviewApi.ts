import { apiClient } from './apiClient'
import { toApiError } from './apiError'
import type { components, operations } from './contracts/openapi'

export type InterviewSummary = components['schemas']['GetInterviewsResponse']
export type CreateInterviewRequest = components['schemas']['CreateInterviewRequest']
export type CreateInterviewResponse = components['schemas']['CreateInterviewResponse']
export type StartInterviewResponse = components['schemas']['StartInterviewResponse']
export type GetInterviewResponse = components['schemas']['GetInterviewResponse']
export type SubmitAnswerResponse = components['schemas']['SubmitAnswerResponse']
export type SubmitAnswerRequest = components['schemas']['SubmitAnswerRequest']

type GetInterviewsQuery = operations['GetInterviews']['parameters']['query']
export type InterviewStatusFilter = NonNullable<NonNullable<GetInterviewsQuery>['status']>[number]

export interface GetInterviewsOptions {
  status?: InterviewStatusFilter[]
  signal?: AbortSignal
}

export async function getInterviews(options?: GetInterviewsOptions): Promise<InterviewSummary[]> {
  try {
    const params = options?.status !== undefined ? { status: options.status } : undefined

    const response = await apiClient.get<InterviewSummary[]>('/interviews', {
      ...(params !== undefined && { params }),
      ...(options?.signal !== undefined && { signal: options.signal }),
    })

    return response.data
  } catch (error) {
    throw toApiError(error)
  }
}

export async function createInterview(
  request: CreateInterviewRequest,
  signal?: AbortSignal
): Promise<CreateInterviewResponse> {
  try {
    const response = await apiClient.post<CreateInterviewResponse>('/interviews', request, {
      ...(signal !== undefined && { signal }),
    })

    return response.data
  } catch (error) {
    throw toApiError(error)
  }
}

export async function getInterview(
  id: string,
  signal?: AbortSignal
): Promise<GetInterviewResponse> {
  try {
    const response = await apiClient.get<GetInterviewResponse>(`/interviews/${id}`, {
      ...(signal !== undefined && { signal }),
    })
    return response.data
  } catch (error) {
    throw toApiError(error)
  }
}

export async function startInterview(
  id: string,
  signal?: AbortSignal
): Promise<StartInterviewResponse> {
  try {
    const response = await apiClient.post<StartInterviewResponse>(
      `/interviews/${id}/start`,
      undefined,
      {
        ...(signal !== undefined && { signal }),
      }
    )
    return response.data
  } catch (error) {
    throw toApiError(error)
  }
}

export async function submitAnswer(
  id: string,
  request: SubmitAnswerRequest,
  signal?: AbortSignal
): Promise<SubmitAnswerResponse> {
  try {
    const response = await apiClient.post<SubmitAnswerResponse>(
      `/interviews/${id}/answers`,
      request,
      {
        ...(signal !== undefined && { signal }),
      }
    )
    return response.data
  } catch (error) {
    throw toApiError(error)
  }
}

export async function completeInterview(id: string, signal?: AbortSignal): Promise<void> {
  try {
    await apiClient.post(`/interviews/${id}/complete`, undefined, {
      ...(signal !== undefined && { signal }),
    })
  } catch (error) {
    throw toApiError(error)
  }
}
