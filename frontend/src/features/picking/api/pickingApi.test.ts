import { beforeEach, describe, expect, it, vi } from 'vitest'

const apiMocks = vi.hoisted(() => ({ GET: vi.fn(), POST: vi.fn() }))
vi.mock('../../../api/client', () => ({ apiClient: apiMocks }))

import { mutatePickingItem } from './pickingApi'

describe('pickingApi', () => {
  beforeEach(() => {
    apiMocks.GET.mockReset()
    apiMocks.POST.mockReset()
    apiMocks.GET.mockResolvedValue({
      data: { token: 'csrf-token', headerName: 'X-CSRF-TOKEN' },
      response: new Response(null, { status: 200 }),
    })
    apiMocks.POST.mockResolvedValue({ data: { id: 'order-1' }, response: new Response(null, { status: 200 }) })
    vi.spyOn(crypto, 'randomUUID').mockReturnValue('11111111-1111-4111-8111-111111111111')
  })

  it('packs an item with an idempotency key, current version and CSRF protection', async () => {
    await mutatePickingItem('order-1', 'item-1', 7, 'pack', 2.5)

    expect(apiMocks.POST).toHaveBeenCalledWith('/api/picking/orders/{orderId}/items/{itemId}/pack', {
      params: { path: { orderId: 'order-1', itemId: 'item-1' } },
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
      body: {
        operationId: '11111111-1111-4111-8111-111111111111',
        itemVersion: 7,
        packedQuantity: 2.5,
      },
    })
  })

  it('reserves an item using the dedicated endpoint', async () => {
    await mutatePickingItem('order-1', 'item-1', 3, 'reserve')

    expect(apiMocks.POST).toHaveBeenCalledWith('/api/picking/orders/{orderId}/items/{itemId}/reserve', {
      params: { path: { orderId: 'order-1', itemId: 'item-1' } },
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
      body: { operationId: '11111111-1111-4111-8111-111111111111', itemVersion: 3 },
    })
  })
})
