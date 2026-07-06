import type { components } from '../../../api/schema'
import { ApiRequestError } from '../../../api/apiError'
import { apiClient } from '../../../api/client'
import { getCsrfHeader } from '../../../api/csrf'

export type Order = components['schemas']['OrderDto']
export type OrderListItem = components['schemas']['OrderListItemDto']
export type OrdersPage = components['schemas']['PagedResultOfOrderListItemDto']
export type CreateOrderInput = components['schemas']['CreateOrderRequest']
export type ProductListItem = components['schemas']['ProductListItemResponse']
export type AvailableOrderAssignee = components['schemas']['AvailableOrderAssigneeDto']

function requireData<T>(data: T | undefined, response: Response, error?: unknown): T {
  if (!response.ok || !data) {
    const detail = typeof error === 'object' && error !== null && 'detail' in error
      && typeof error.detail === 'string' ? error.detail : undefined
    throw new ApiRequestError(response.status, detail)
  }
  return data
}

export async function getOrders(page: number, pageSize: number): Promise<OrdersPage> {
  const { data, error, response } = await apiClient.GET('/api/orders', {
    params: { query: { page, pageSize } },
  })
  return requireData(data, response, error)
}

export async function getOrder(id: string): Promise<Order> {
  const { data, error, response } = await apiClient.GET('/api/orders/{id}', {
    params: { path: { id } },
  })
  return requireData(data, response, error)
}

export async function getAvailableOrderAssignees(): Promise<AvailableOrderAssignee[]> {
  const { data, error, response } = await apiClient.GET('/api/orders/available-assignees')
  return [...requireData(data, response, error)]
}

export async function createOrder(body: CreateOrderInput): Promise<Order> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/orders', { body, headers })
  return requireData(data, response, error)
}

export async function addOrderItem(orderId: string, productId: number, quantity: number, version: number): Promise<Order> {
  try {
    return await addOrderItemOnce(orderId, productId, quantity, version)
  } catch (error) {
    if (!(error instanceof ApiRequestError) || error.status !== 409) throw error
    const current = await getOrder(orderId)
    if (current.items.some((item) => Number(item.productId) === productId)) return current
    return addOrderItemOnce(orderId, productId, quantity, Number(current.version))
  }
}

async function addOrderItemOnce(orderId: string, productId: number, quantity: number, version: number): Promise<Order> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/orders/{id}/items', {
    params: { path: { id: orderId } },
    body: { productId, quantity, version },
    headers,
  })
  return requireData(data, response, error)
}

export async function publishOrder(orderId: string, version: number): Promise<Order> {
  try {
    return await publishOrderOnce(orderId, version)
  } catch (error) {
    if (!(error instanceof ApiRequestError) || error.status !== 409) throw error
    const current = await getOrder(orderId)
    if (isPublished(current.status)) return current
    return publishOrderOnce(orderId, Number(current.version))
  }
}

export async function configureOrderPicking(orderId: string, pickingMode: number,
  employeeIds: string[], version: number): Promise<Order> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.PUT('/api/orders/{id}/picking-configuration', {
    params: { path: { id: orderId } }, body: { pickingMode, employeeIds, version }, headers,
  })
  return requireData(data, response, error)
}

export async function deleteOrder(orderId: string, version: number): Promise<void> {
  const headers = await getCsrfHeader()
  const { error, response } = await apiClient.DELETE('/api/orders/{id}', {
    params: { path: { id: orderId }, query: { version } }, headers,
  })
  if (!response.ok) requireData(undefined, response, error)
}

async function publishOrderOnce(orderId: string, version: number): Promise<Order> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/orders/{id}/publish', {
    params: { path: { id: orderId } }, body: { version }, headers,
  })
  return requireData(data, response, error)
}

export async function searchProducts(search: string): Promise<ProductListItem[]> {
  const { data, error, response } = await apiClient.GET('/api/products', {
    params: { query: { Search: search, Page: 1, PageSize: 10 } },
  })
  return requireData(data, response, error).items
}

function isPublished(status: number | string) {
  return status === 1 || status === 'ReadyForPicking'
}
