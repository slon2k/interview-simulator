import { apiClient } from '../../api/apiClient'
import { toApiError } from '../../api/apiError'
import type { CurrentUser } from './authTypes'

export async function getCurrentUser(signal?: AbortSignal): Promise<CurrentUser> {
  try {
    const response = await apiClient.get<CurrentUser>('/me', {
      ...(signal !== undefined && { signal })
    })

    return response.data
  } catch (error) {
    throw toApiError(error)
  }
}

export async function logoutCurrentUser(): Promise<void> {
  try {
    await apiClient.post<void>('/auth/logout')
  } catch (error) {
    throw toApiError(error)
  }
}

export function buildLoginUrl(returnUrl: string = "/"): string {
  const safeReturnUrl = normalizeReturnUrl(returnUrl);

  return `/api/auth/login?returnUrl=${encodeURIComponent(safeReturnUrl)}`;
}

function normalizeReturnUrl(returnUrl: string): string {
  if (!returnUrl.startsWith("/")) {
    return "/";
  }

  if (returnUrl.startsWith("//")) {
    return "/";
  }

  if (returnUrl.includes("\\")) {
    return "/";
  }

  return returnUrl;
}