import { apiClient } from './apiClient'

export type HealthResponse = {
  status: string
}

export async function getHealth(): Promise<HealthResponse> {
  const response = await apiClient.get<HealthResponse>('/health')
  return response.data
}
