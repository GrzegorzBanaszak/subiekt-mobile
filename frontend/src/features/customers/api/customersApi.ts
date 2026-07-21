import type { components } from '../../../api/schema'
import { ApiRequestError } from '../../../api/apiError'
import { apiClient } from '../../../api/client'
import { getCsrfHeader } from '../../../api/csrf'

export type Customer = components['schemas']['CustomerDto']
export type CustomerListItem = components['schemas']['CustomerListItemDto']
export type CustomerPage = components['schemas']['PagedResultOfCustomerListItemDto']
export type CustomerSite = components['schemas']['CustomerSiteDto']
export type CustomerSitePage = components['schemas']['PagedResultOfCustomerSiteListItemDto']
export type CustomerActivityPage = components['schemas']['PagedResultOfCustomerActivityDto']
export type CustomerContractorPage = components['schemas']['PagedResultOfCustomerContractorDto']
export type CreateCustomerInput = components['schemas']['CreateCustomerRequest']
export type UpdateCustomerInput = components['schemas']['UpdateCustomerRequest']
export type CreateCustomerSiteInput = components['schemas']['CreateCustomerSiteRequest']
export type UpdateCustomerSiteInput = components['schemas']['UpdateCustomerSiteRequest']
export type LogisticsProfileInput = components['schemas']['ConfigureCustomerSiteLogisticsProfileRequest']

function requireData<T>(data: T | undefined, response: Response, error?: unknown): T {
  if (!response.ok || !data) {
    const detail = typeof error === 'object' && error !== null && 'detail' in error
      && typeof error.detail === 'string' ? error.detail : undefined
    throw new ApiRequestError(response.status, detail)
  }
  return data
}

export async function getCustomers(search: string, isActive: boolean | undefined, page: number, pageSize: number): Promise<CustomerPage> {
  const { data, error, response } = await apiClient.GET('/api/customers', {
    params: { query: { search: search || undefined, isActive, page, pageSize } },
  })
  return requireData(data, response, error)
}

export async function getCustomer(customerId: string): Promise<Customer> {
  const { data, error, response } = await apiClient.GET('/api/customers/{customerId}', {
    params: { path: { customerId } },
  })
  return requireData(data, response, error)
}

export async function createCustomer(body: CreateCustomerInput): Promise<Customer> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/customers', { body, headers })
  return requireData(data, response, error)
}

export async function updateCustomer(customerId: string, body: UpdateCustomerInput): Promise<Customer> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.PUT('/api/customers/{customerId}', {
    params: { path: { customerId } }, body, headers,
  })
  return requireData(data, response, error)
}

export async function setCustomerActive(customerId: string, isActive: boolean, version: number | string): Promise<Customer> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.PUT('/api/customers/{customerId}/active', {
    params: { path: { customerId } }, body: { isActive, version }, headers,
  })
  return requireData(data, response, error)
}

export async function getCustomerSites(customerId: string, search: string, page: number, pageSize: number): Promise<CustomerSitePage> {
  const { data, error, response } = await apiClient.GET('/api/customers/{customerId}/sites', {
    params: { path: { customerId }, query: { search: search || undefined, page, pageSize } },
  })
  return requireData(data, response, error)
}

export async function getCustomerSite(customerId: string, siteId: string): Promise<CustomerSite> {
  const { data, error, response } = await apiClient.GET('/api/customers/{customerId}/sites/{siteId}', {
    params: { path: { customerId, siteId } },
  })
  return requireData(data, response, error)
}

export async function createCustomerSite(customerId: string, body: CreateCustomerSiteInput): Promise<CustomerSite> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/customers/{customerId}/sites', {
    params: { path: { customerId } }, body, headers,
  })
  return requireData(data, response, error)
}

export async function updateCustomerSite(customerId: string, siteId: string, body: UpdateCustomerSiteInput): Promise<CustomerSite> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.PUT('/api/customers/{customerId}/sites/{siteId}', {
    params: { path: { customerId, siteId } }, body, headers,
  })
  return requireData(data, response, error)
}

export async function setCustomerSiteActive(customerId: string, siteId: string, isActive: boolean, version: number | string): Promise<CustomerSite> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.PUT('/api/customers/{customerId}/sites/{siteId}/active', {
    params: { path: { customerId, siteId } }, body: { isActive, version }, headers,
  })
  return requireData(data, response, error)
}

export async function configureCustomerSiteLogisticsProfile(customerId: string, siteId: string, body: LogisticsProfileInput): Promise<CustomerSite> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.PUT('/api/customers/{customerId}/sites/{siteId}/logistics-profile', {
    params: { path: { customerId, siteId } }, body, headers,
  })
  return requireData(data, response, error)
}

export async function getCustomerActivity(customerId: string, page: number, pageSize: number): Promise<CustomerActivityPage> {
  const { data, error, response } = await apiClient.GET('/api/customers/{customerId}/activity', {
    params: { path: { customerId }, query: { page, pageSize } },
  })
  return requireData(data, response, error)
}

export async function searchCustomerContractors(search: string): Promise<CustomerContractorPage> {
  const { data, error, response } = await apiClient.GET('/api/customer-contractors', {
    params: { query: { search: search || undefined, page: 1, pageSize: 10 } },
  })
  return requireData(data, response, error)
}
