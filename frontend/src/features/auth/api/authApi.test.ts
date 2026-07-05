import { beforeEach, describe, expect, it, vi } from 'vitest'

const apiMocks = vi.hoisted(() => ({
  GET: vi.fn(),
  POST: vi.fn(),
}))

vi.mock('../../../api/client', () => ({ apiClient: apiMocks }))

import { signInAdministrator } from './authApi'

describe('authApi', () => {
  beforeEach(() => {
    apiMocks.GET.mockReset()
    apiMocks.POST.mockReset()
  })

  it('obtains a CSRF token and sends it with administrator credentials', async () => {
    const actor = {
      kind: 'Administrator',
      id: '66a787d1-1857-4ef0-a7d9-73dbfdfd7573',
      organizationId: null,
      displayName: 'Administrator',
      permissions: ['identity.manage'],
      sessionId: '37e6300c-7011-4539-a062-4bb3a8650970',
    }

    apiMocks.GET.mockResolvedValue({
      data: { token: 'csrf-token', headerName: 'X-CSRF-TOKEN' },
      response: new Response(null, { status: 200 }),
    })
    apiMocks.POST.mockResolvedValue({
      data: { expiresAtUtc: '2026-07-05T12:00:00Z', actor },
      response: new Response(null, { status: 200 }),
    })

    await expect(
      signInAdministrator({
        username: 'admin',
        password: 'secret-password',
      }),
    ).resolves.toEqual(actor)

    expect(apiMocks.GET).toHaveBeenCalledWith('/api/auth/csrf-token')
    expect(apiMocks.POST).toHaveBeenCalledWith(
      '/api/auth/administrator/sign-in',
      {
        body: { username: 'admin', password: 'secret-password' },
        headers: { 'X-CSRF-TOKEN': 'csrf-token' },
      },
    )
  })
})
