import { beforeEach, describe, expect, it, vi } from 'vitest'

const apiMocks = vi.hoisted(() => ({
  GET: vi.fn(),
  POST: vi.fn(),
}))

vi.mock('../../../api/client', () => ({ apiClient: apiMocks }))

import { changeOwnPassword, signInAdministrator, signOut } from './authApi'

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
      requiresPasswordChange: false,
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

  it('signs out with CSRF protection', async () => {
    apiMocks.GET.mockResolvedValue({
      data: { token: 'csrf-token', headerName: 'X-CSRF-TOKEN' },
      response: new Response(null, { status: 200 }),
    })
    apiMocks.POST.mockResolvedValue({
      response: new Response(null, { status: 204 }),
    })

    await expect(signOut()).resolves.toBeUndefined()

    expect(apiMocks.POST).toHaveBeenCalledWith('/api/auth/sign-out', {
      headers: { 'X-CSRF-TOKEN': 'csrf-token' },
    })
  })

  it('changes the current administrator password with CSRF protection', async () => {
    apiMocks.GET.mockResolvedValue({
      data: { token: 'csrf-token', headerName: 'X-CSRF-TOKEN' },
      response: new Response(null, { status: 200 }),
    })
    apiMocks.POST.mockResolvedValue({
      response: new Response(null, { status: 204 }),
    })

    await expect(changeOwnPassword({
      currentPassword: 'temporary-password',
      newPassword: 'new-secure-password',
    })).resolves.toBeUndefined()

    expect(apiMocks.POST).toHaveBeenCalledWith(
      '/api/auth/administrator/change-password',
      {
        body: {
          currentPassword: 'temporary-password',
          newPassword: 'new-secure-password',
        },
        headers: { 'X-CSRF-TOKEN': 'csrf-token' },
      },
    )
  })
})
