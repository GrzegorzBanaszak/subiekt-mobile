import { beforeEach, describe, expect, it, vi } from 'vitest'

const apiMocks = vi.hoisted(() => ({ GET: vi.fn(), POST: vi.fn(), PUT: vi.fn() }))
vi.mock('../../../api/client', () => ({ apiClient: apiMocks }))

import { createAdministrator, setEmployeeActive } from './administrationApi'

describe('administrationApi', () => {
  beforeEach(() => {
    apiMocks.GET.mockReset()
    apiMocks.POST.mockReset()
    apiMocks.PUT.mockReset()
    apiMocks.GET.mockResolvedValue({
      data: { token: 'csrf-token', headerName: 'X-CSRF-TOKEN' },
      response: new Response(null, { status: 200 }),
    })
  })

  it('sends CSRF token while creating an administrator', async () => {
    const administrator = {
      id: 'admin-1', username: 'admin2', displayName: 'Admin 2', isActive: true,
      isBootstrapAdministrator: false, requiresPasswordChange: true, createdAtUtc: '2026-07-05T12:00:00Z', updatedAtUtc: '2026-07-05T12:00:00Z',
    }
    const result = { administrator, temporaryPassword: 'Generated-Password-42!' }
    apiMocks.POST.mockResolvedValue({ data: result, response: new Response(null, { status: 201 }) })

    await expect(createAdministrator({ username: 'admin2', displayName: 'Admin 2' })).resolves.toEqual(result)
    expect(apiMocks.POST).toHaveBeenCalledWith('/api/admin/administrators', {
      body: { username: 'admin2', displayName: 'Admin 2' },
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
    })
  })

  it('sends scoped employee activation request with CSRF token', async () => {
    apiMocks.PUT.mockResolvedValue({ response: new Response(null, { status: 204 }) })

    await setEmployeeActive('org-1', 'employee-1', false)
    expect(apiMocks.PUT).toHaveBeenCalledWith(
      '/api/admin/organizations/{organizationId}/employees/{employeeId}/active',
      {
        params: { path: { organizationId: 'org-1', employeeId: 'employee-1' } },
        body: { isActive: false },
        headers: { 'X-CSRF-TOKEN': 'csrf-token' },
      },
    )
  })
})
