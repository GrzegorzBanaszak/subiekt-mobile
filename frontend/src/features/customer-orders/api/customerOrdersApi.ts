import type { components } from '../../../api/schema'
import { ApiRequestError } from '../../../api/apiError'
import { apiClient } from '../../../api/client'
import { getCsrfHeader } from '../../../api/csrf'

export type CustomerOrder = components['schemas']['SubiektCustomerOrderDto']
export type CustomerOrderListItem = components['schemas']['SubiektCustomerOrderListItemDto']
export type CustomerOrdersPage = components['schemas']['PagedResultOfSubiektCustomerOrderListItemDto']
export type CustomerOrderConversion = components['schemas']['SubiektCustomerOrderConversionDto']

function requireData<T>(data: T | undefined, response: Response, error?: unknown): T {
  if (!response.ok || !data) {
    const detail = typeof error === 'object' && error !== null && 'detail' in error && typeof error.detail === 'string'
      ? error.detail : undefined
    throw new ApiRequestError(response.status, detail)
  }
  return data
}

export async function getCustomerOrders(input: { search?: string; includeCompleted?: boolean; page: number; pageSize: number }): Promise<CustomerOrdersPage> {
  const { data, error, response } = await apiClient.GET('/api/customer-orders', { params: { query: input } })
  return requireData(data, response, error)
}

export async function getCustomerOrder(sourceDocumentId: number): Promise<CustomerOrder> {
  const { data, error, response } = await apiClient.GET('/api/customer-orders/{sourceDocumentId}', {
    params: { path: { sourceDocumentId } },
  })
  return requireData(data, response, error)
}

export async function convertCustomerOrder(sourceDocumentId: number): Promise<CustomerOrderConversion> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/customer-orders/{sourceDocumentId}/convert', {
    params: { path: { sourceDocumentId } }, headers,
  })
  return requireData(data, response, error)
}
