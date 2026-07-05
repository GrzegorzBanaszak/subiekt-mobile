import { apiClient } from './client'
import { ApiRequestError } from './apiError'

export async function getCsrfHeader() {
  const { data, response } = await apiClient.GET('/api/auth/csrf-token')

  if (!response.ok || !data) {
    throw new ApiRequestError(response.status)
  }

  return { [data.headerName]: data.token }
}
