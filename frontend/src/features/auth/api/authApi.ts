import type { components } from '../../../api/schema'
import { apiClient } from '../../../api/client'

export type CurrentActor = components['schemas']['CurrentActor']
export type SignInCredentials = components['schemas']['AdministratorSignInRequest']

export class AuthApiError extends Error {
  constructor(public readonly status: number) {
    super(`Authentication request failed with status ${status}.`)
  }
}

async function getCsrfHeader() {
  const { data, response } = await apiClient.GET('/api/auth/csrf-token')

  if (!response.ok || !data) {
    throw new AuthApiError(response.status)
  }

  return { [data.headerName]: data.token }
}

export async function signInAdministrator(credentials: SignInCredentials) {
  const csrfHeader = await getCsrfHeader()
  const { data, response } = await apiClient.POST(
    '/api/auth/administrator/sign-in',
    {
      body: credentials,
      headers: csrfHeader,
    },
  )

  if (!response.ok || !data) {
    throw new AuthApiError(response.status)
  }

  return data.actor
}

export async function getCurrentActor(): Promise<CurrentActor | null> {
  const { data, response } = await apiClient.GET('/api/auth/me')

  if (response.status === 401) {
    return null
  }

  if (!response.ok || !data) {
    throw new AuthApiError(response.status)
  }

  return data
}
