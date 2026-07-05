import type { components } from '../../../api/schema'
import { ApiRequestError } from '../../../api/apiError'
import { apiClient } from '../../../api/client'
import { getCsrfHeader } from '../../../api/csrf'

export type Administrator = components['schemas']['AdministratorDto']
export type Organization = components['schemas']['OrganizationDto']
export type Employee = components['schemas']['EmployeeDto']
export type CreateAdministratorInput = components['schemas']['CreateAdministratorRequest']
export type CreatedAdministrator = components['schemas']['CreatedAdministratorDto']
export type UpdateAdministratorInput = components['schemas']['UpdateAdministratorRequest']
export type CreateOrganizationInput = components['schemas']['CreateOrganizationRequest']
export type UpdateOrganizationInput = components['schemas']['UpdateOrganizationRequest']
export type CreateEmployeeInput = components['schemas']['CreateEmployeeRequest']
export type UpdateEmployeeInput = components['schemas']['UpdateEmployeeRequest']

function assertResponse(response: Response) {
  if (!response.ok) throw new ApiRequestError(response.status)
}

export async function getAdministrators(): Promise<Administrator[]> {
  const { data, response } = await apiClient.GET('/api/admin/administrators')
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return [...data]
}

export async function createAdministrator(body: CreateAdministratorInput) {
  const headers = await getCsrfHeader()
  const { data, response } = await apiClient.POST('/api/admin/administrators', { body, headers })
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return data
}

export async function updateAdministrator(id: string, body: UpdateAdministratorInput) {
  const headers = await getCsrfHeader()
  const { data, response } = await apiClient.PUT('/api/admin/administrators/{id}', {
    params: { path: { id } }, body, headers,
  })
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return data
}

export async function resetAdministratorPassword(id: string, password: string) {
  const headers = await getCsrfHeader()
  const { response } = await apiClient.POST('/api/admin/administrators/{id}/reset-password', {
    params: { path: { id } }, body: { password }, headers,
  })
  assertResponse(response)
}

export async function setAdministratorActive(id: string, isActive: boolean) {
  const headers = await getCsrfHeader()
  const { response } = await apiClient.PUT('/api/admin/administrators/{id}/active', {
    params: { path: { id } }, body: { isActive }, headers,
  })
  assertResponse(response)
}

export async function getOrganizations(): Promise<Organization[]> {
  const { data, response } = await apiClient.GET('/api/admin/organizations')
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return [...data]
}

export async function createOrganization(body: CreateOrganizationInput) {
  const headers = await getCsrfHeader()
  const { data, response } = await apiClient.POST('/api/admin/organizations', { body, headers })
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return data
}

export async function updateOrganization(id: string, body: UpdateOrganizationInput) {
  const headers = await getCsrfHeader()
  const { data, response } = await apiClient.PUT('/api/admin/organizations/{id}', {
    params: { path: { id } }, body, headers,
  })
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return data
}

export async function setOrganizationActive(id: string, isActive: boolean) {
  const headers = await getCsrfHeader()
  const { response } = await apiClient.PUT('/api/admin/organizations/{id}/active', {
    params: { path: { id } }, body: { isActive }, headers,
  })
  assertResponse(response)
}

export async function getEmployees(organizationId: string): Promise<Employee[]> {
  const { data, response } = await apiClient.GET('/api/admin/organizations/{organizationId}/employees', {
    params: { path: { organizationId } },
  })
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return [...data]
}

export async function createEmployee(organizationId: string, body: CreateEmployeeInput) {
  const headers = await getCsrfHeader()
  const { data, response } = await apiClient.POST('/api/admin/organizations/{organizationId}/employees', {
    params: { path: { organizationId } }, body, headers,
  })
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return data
}

export async function updateEmployee(
  organizationId: string,
  employeeId: string,
  body: UpdateEmployeeInput,
) {
  const headers = await getCsrfHeader()
  const { data, response } = await apiClient.PUT(
    '/api/admin/organizations/{organizationId}/employees/{employeeId}',
    { params: { path: { organizationId, employeeId } }, body, headers },
  )
  assertResponse(response)
  if (!data) throw new ApiRequestError(response.status)
  return data
}

export async function setEmployeeActive(
  organizationId: string,
  employeeId: string,
  isActive: boolean,
) {
  const headers = await getCsrfHeader()
  const { response } = await apiClient.PUT(
    '/api/admin/organizations/{organizationId}/employees/{employeeId}/active',
    { params: { path: { organizationId, employeeId } }, body: { isActive }, headers },
  )
  assertResponse(response)
}
