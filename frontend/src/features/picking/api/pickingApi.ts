import type { components } from '../../../api/schema'
import { ApiRequestError } from '../../../api/apiError'
import { apiClient } from '../../../api/client'
import { getCsrfHeader } from '../../../api/csrf'

export type PickingWarehouseOrderListItem = components['schemas']['PickingWarehouseOrderListItemDto']
export type PickingWarehouseOrdersPage = components['schemas']['PagedResultOfPickingWarehouseOrderListItemDto']
export type PickingWarehouseOrderDetails = components['schemas']['PickingWarehouseOrderDetailsDto']
export type PickingItem = components['schemas']['PickingItemDto']
export type PickingHistoryItem = components['schemas']['PickingHistoryItemDto']
export type PickingHistoryPage = components['schemas']['PagedResultOfPickingHistoryItemDto']

export interface PickingFilters {
  page: number
  pageSize: number
  search?: string
  status?: number
  dueDateFrom?: string
  dueDateTo?: string
  customer?: string
}

function requireData<T>(data: T | undefined, response: Response, error?: unknown): T {
  if (!response.ok || !data) {
    const detail = typeof error === 'object' && error !== null && 'detail' in error
      && typeof error.detail === 'string' ? error.detail : undefined
    throw new ApiRequestError(response.status, detail)
  }
  return data
}

export async function getPickingWarehouseOrders(filters: PickingFilters): Promise<PickingWarehouseOrdersPage> {
  const { data, error, response } = await apiClient.GET('/api/picking/warehouse-orders', {
    params: { query: filters },
  })
  return requireData(data, response, error)
}

export async function getPickingWarehouseOrder(warehouseOrderId: string): Promise<PickingWarehouseOrderDetails> {
  const { data, error, response } = await apiClient.GET('/api/picking/warehouse-orders/{warehouseOrderId}', {
    params: { path: { warehouseOrderId } },
  })
  return requireData(data, response, error)
}

export async function getPickingHistory(warehouseOrderId: string, page: number, pageSize = 20): Promise<PickingHistoryPage> {
  const { data, error, response } = await apiClient.GET('/api/picking/warehouse-orders/{warehouseOrderId}/history', {
    params: { path: { warehouseOrderId }, query: { page, pageSize } },
  })
  return requireData(data, response, error)
}

export type PickingMutation = 'reserve' | 'release' | 'pack' | 'undo-pack'

export async function mutatePickingItem(warehouseOrderId: string, itemId: string, itemVersion: number,
  action: PickingMutation, packedQuantity?: number): Promise<PickingWarehouseOrderDetails> {
  const operationId = crypto.randomUUID()
  const headers = await getCsrfHeader()
  const params = { path: { warehouseOrderId, itemId } }
  if (action === 'pack') {
    const { data, error, response } = await apiClient.POST('/api/picking/warehouse-orders/{warehouseOrderId}/items/{itemId}/pack', {
      params, headers, body: { operationId, itemVersion, packedQuantity: packedQuantity ?? 0 },
    })
    return requireData(data, response, error)
  }
  const body = { operationId, itemVersion }
  const result = action === 'reserve'
    ? await apiClient.POST('/api/picking/warehouse-orders/{warehouseOrderId}/items/{itemId}/reserve', { params, headers, body })
    : action === 'release'
      ? await apiClient.POST('/api/picking/warehouse-orders/{warehouseOrderId}/items/{itemId}/release', { params, headers, body })
      : await apiClient.POST('/api/picking/warehouse-orders/{warehouseOrderId}/items/{itemId}/undo-pack', { params, headers, body })
  return requireData(result.data, result.response, result.error)
}
