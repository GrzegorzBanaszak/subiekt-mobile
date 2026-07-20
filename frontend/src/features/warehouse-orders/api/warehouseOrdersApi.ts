import type { components } from '../../../api/schema'
import { ApiRequestError } from '../../../api/apiError'
import { apiClient } from '../../../api/client'
import { getCsrfHeader } from '../../../api/csrf'

export type WarehouseOrder = components['schemas']['WarehouseOrderDto']
export type WarehouseOrderListItem = components['schemas']['WarehouseOrderListItemDto']
export type WarehouseOrdersPage = components['schemas']['PagedResultOfWarehouseOrderListItemDto']
export type CreateWarehouseOrderInput = components['schemas']['CreateWarehouseOrderRequest']
export type ProductListItem = components['schemas']['ProductListItemResponse']
export type AvailableWarehouseOrderAssignee = components['schemas']['AvailableWarehouseOrderAssigneeDto']

function requireData<T>(data: T | undefined, response: Response, error?: unknown): T {
  if (!response.ok || !data) {
    const detail = typeof error === 'object' && error !== null && 'detail' in error
      && typeof error.detail === 'string' ? error.detail : undefined
    throw new ApiRequestError(response.status, detail)
  }
  return data
}

export async function getWarehouseOrders(page: number, pageSize: number): Promise<WarehouseOrdersPage> {
  const { data, error, response } = await apiClient.GET('/api/warehouse-orders', {
    params: { query: { page, pageSize } },
  })
  return requireData(data, response, error)
}

export async function getWarehouseOrder(id: string): Promise<WarehouseOrder> {
  const { data, error, response } = await apiClient.GET('/api/warehouse-orders/{id}', {
    params: { path: { id } },
  })
  return requireData(data, response, error)
}

export async function getAvailableWarehouseOrderAssignees(): Promise<AvailableWarehouseOrderAssignee[]> {
  const { data, error, response } = await apiClient.GET('/api/warehouse-orders/available-assignees')
  return [...requireData(data, response, error)]
}

export async function createWarehouseOrder(body: CreateWarehouseOrderInput): Promise<WarehouseOrder> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/warehouse-orders', { body, headers })
  return requireData(data, response, error)
}

export async function addWarehouseOrderItem(warehouseOrderId: string, productId: number, quantity: number, version: number): Promise<WarehouseOrder> {
  try {
    return await addWarehouseOrderItemOnce(warehouseOrderId, productId, quantity, version)
  } catch (error) {
    if (!(error instanceof ApiRequestError) || error.status !== 409) throw error
    const current = await getWarehouseOrder(warehouseOrderId)
    if (current.items.some((item) => Number(item.productId) === productId)) return current
    return addWarehouseOrderItemOnce(warehouseOrderId, productId, quantity, Number(current.version))
  }
}

async function addWarehouseOrderItemOnce(warehouseOrderId: string, productId: number, quantity: number, version: number): Promise<WarehouseOrder> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/warehouse-orders/{id}/items', {
    params: { path: { id: warehouseOrderId } },
    body: { productId, quantity, version },
    headers,
  })
  return requireData(data, response, error)
}

export async function publishWarehouseOrder(warehouseOrderId: string, version: number): Promise<WarehouseOrder> {
  try {
    return await publishWarehouseOrderOnce(warehouseOrderId, version)
  } catch (error) {
    if (!(error instanceof ApiRequestError) || error.status !== 409) throw error
    const current = await getWarehouseOrder(warehouseOrderId)
    if (isPublished(current.status)) return current
    return publishWarehouseOrderOnce(warehouseOrderId, Number(current.version))
  }
}

export async function configureWarehouseOrderPicking(warehouseOrderId: string, pickingMode: number,
  employeeIds: string[], version: number): Promise<WarehouseOrder> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.PUT('/api/warehouse-orders/{id}/picking-configuration', {
    params: { path: { id: warehouseOrderId } }, body: { pickingMode, employeeIds, version }, headers,
  })
  return requireData(data, response, error)
}

export async function deleteWarehouseOrder(warehouseOrderId: string, version: number): Promise<void> {
  const headers = await getCsrfHeader()
  const { error, response } = await apiClient.DELETE('/api/warehouse-orders/{id}', {
    params: { path: { id: warehouseOrderId }, query: { version } }, headers,
  })
  if (!response.ok) requireData(undefined, response, error)
}

async function publishWarehouseOrderOnce(warehouseOrderId: string, version: number): Promise<WarehouseOrder> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/warehouse-orders/{id}/publish', {
    params: { path: { id: warehouseOrderId } }, body: { version }, headers,
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
