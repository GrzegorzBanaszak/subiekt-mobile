import { beforeEach, describe, expect, it, vi } from 'vitest'

const apiMocks = vi.hoisted(() => ({ GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() }))
vi.mock('../../../api/client', () => ({ apiClient: apiMocks }))

import { addOrderItem, configureOrderPicking, createOrder, deleteOrder, publishOrder } from './ordersApi'

const order = {
  id: '11111111-1111-1111-1111-111111111111', number: 'ZAM-1', customerName: 'Klient',
  dueDate: '2026-07-10', status: 0, createdById: '22222222-2222-2222-2222-222222222222',
  createdByName: 'Admin', createdAtUtc: '2026-07-05T10:00:00Z',
  updatedById: '22222222-2222-2222-2222-222222222222', updatedByName: 'Admin',
  updatedAtUtc: '2026-07-05T10:00:00Z', publishedAtUtc: null, version: 1, items: [],
}

describe('ordersApi', () => {
  beforeEach(() => {
    apiMocks.GET.mockReset()
    apiMocks.POST.mockReset()
    apiMocks.DELETE.mockReset()
    apiMocks.PUT.mockReset()
    apiMocks.GET.mockResolvedValue({
      data: { token: 'csrf-token', headerName: 'X-CSRF-TOKEN' },
      response: new Response(null, { status: 200 }),
    })
    apiMocks.POST.mockResolvedValue({ data: order, response: new Response(null, { status: 200 }) })
  })

  it('deletes a draft with its current version and CSRF protection', async () => {
    apiMocks.DELETE.mockResolvedValue({ response: new Response(null, { status: 204 }) })

    await deleteOrder(order.id, 3)

    expect(apiMocks.DELETE).toHaveBeenCalledWith('/api/orders/{id}', {
      params: { path: { id: order.id }, query: { version: 3 } },
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
    })
  })

  it('updates picking mode and assignees with the current version', async () => {
    apiMocks.PUT.mockResolvedValue({ data: order, response: new Response(null, { status: 200 }) })

    await configureOrderPicking(order.id, 1, ['employee-1', 'employee-2'], 4)

    expect(apiMocks.PUT).toHaveBeenCalledWith('/api/orders/{id}/picking-configuration', {
      params: { path: { id: order.id } },
      body: { pickingMode: 1, employeeIds: ['employee-1', 'employee-2'], version: 4 },
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
    })
  })

  it('creates an order with CSRF protection', async () => {
    await createOrder({ customerName: 'Klient', dueDate: '2026-07-10', pickingMode: 0, employeeIds: ['employee-1'] })
    expect(apiMocks.POST).toHaveBeenCalledWith('/api/orders', {
      body: { customerName: 'Klient', dueDate: '2026-07-10', pickingMode: 0, employeeIds: ['employee-1'] },
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
    })
  })

  it('passes the current version while adding an item and publishing', async () => {
    await addOrderItem(order.id, 7, 2.5, 4)
    await publishOrder(order.id, 5)

    expect(apiMocks.POST).toHaveBeenNthCalledWith(1, '/api/orders/{id}/items', {
      params: { path: { id: order.id } }, body: { productId: 7, quantity: 2.5, version: 4 },
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
    })
    expect(apiMocks.POST).toHaveBeenNthCalledWith(2, '/api/orders/{id}/publish', {
      params: { path: { id: order.id } }, body: { version: 5 },
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
    })
  })

  it('reloads the order and retries an item after a version conflict', async () => {
    apiMocks.GET
      .mockResolvedValueOnce({ data: { token: 'csrf-1', headerName: 'X-CSRF-TOKEN' }, response: new Response(null, { status: 200 }) })
      .mockResolvedValueOnce({ data: { ...order, version: 3 }, response: new Response(null, { status: 200 }) })
      .mockResolvedValueOnce({ data: { token: 'csrf-2', headerName: 'X-CSRF-TOKEN' }, response: new Response(null, { status: 200 }) })
    apiMocks.POST
      .mockResolvedValueOnce({ error: { detail: 'Order was modified by another request.' }, response: new Response(null, { status: 409 }) })
      .mockResolvedValueOnce({ data: { ...order, version: 4 }, response: new Response(null, { status: 200 }) })

    await expect(addOrderItem(order.id, 7, 1, 1)).resolves.toMatchObject({ version: 4 })
    expect(apiMocks.POST).toHaveBeenLastCalledWith('/api/orders/{id}/items', {
      params: { path: { id: order.id } }, body: { productId: 7, quantity: 1, version: 3 },
      headers: { 'X-CSRF-TOKEN': 'csrf-2' },
    })
  })
})
