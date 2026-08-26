import { apiClient } from './apiClient'
import { toApiError } from './apiError'
import type { components } from './contracts/openapi'

export type DashboardResponse = components['schemas']['DashboardResponse']
export type DashboardScoreTrendPointResponse =
  components['schemas']['DashboardScoreTrendPointResponse']
export type DashboardScoreByFocusAreaResponse =
  components['schemas']['DashboardScoreByFocusAreaResponse']
export type DashboardScoreByInterviewTypeResponse =
  components['schemas']['DashboardScoreByInterviewTypeResponse']
export type DashboardScoreByDimensionResponse =
  components['schemas']['DashboardScoreByDimensionResponse']
export type DashboardRecentSessionResponse = components['schemas']['DashboardRecentSessionResponse']

export async function getDashboard(signal?: AbortSignal): Promise<DashboardResponse> {
  try {
    const response = await apiClient.get<DashboardResponse>('/dashboard', {
      ...(signal !== undefined && { signal }),
    })
    return response.data
  } catch (error) {
    throw toApiError(error)
  }
}
