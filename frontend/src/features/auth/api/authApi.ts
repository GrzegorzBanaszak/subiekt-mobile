import type { components } from '../../../api/schema'
import { apiClient } from '../../../api/client'
import { getCsrfHeader } from '../../../api/csrf'

export type CurrentActor = components['schemas']['CurrentActor']
export type SignInCredentials = components['schemas']['AdministratorSignInRequest']
export type PublicOrganization = components['schemas']['PublicOrganizationDto']
export type PublicEmployee = components['schemas']['PublicEmployeeDto']

export class AuthApiError extends Error {
  constructor(public readonly status: number) {
    super(`Authentication request failed with status ${status}.`)
  }
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

export async function signOut(): Promise<void> {
  const csrfHeader = await getCsrfHeader()
  const { response } = await apiClient.POST('/api/auth/sign-out', {
    headers: csrfHeader,
  })

  if (!response.ok) {
    throw new AuthApiError(response.status)
  }
}

export async function getOrganizations(): Promise<PublicOrganization[]> {
  const { data, response } = await apiClient.GET('/api/auth/organizations')

  if (!response.ok || !data) {
    throw new AuthApiError(response.status)
  }

  return [...data]
}

export async function getEmployees(
  organizationId: string,
): Promise<PublicEmployee[]> {
  const { data, response } = await apiClient.GET(
    '/api/auth/organizations/{organizationId}/employees',
    { params: { path: { organizationId } } },
  )

  if (!response.ok || !data) {
    throw new AuthApiError(response.status)
  }

  return [...data]
}

export async function selectEmployee(
  organizationId: string,
  employeeId: string,
): Promise<CurrentActor> {
  const csrfHeader = await getCsrfHeader()
  const { data, response } = await apiClient.POST('/api/auth/employee/select', {
    body: { organizationId, employeeId },
    headers: csrfHeader,
  })

  if (!response.ok || !data) {
    throw new AuthApiError(response.status)
  }

  return data.actor
}
