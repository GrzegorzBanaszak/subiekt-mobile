import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { RouterProvider } from 'react-router-dom'
import { I18nProvider } from '../i18n/I18nProvider'
import { AuthContext } from '../../features/auth/authContext'
import { appRouter } from './appRouter'

afterEach(cleanup)

describe('appRouter', () => {
  it('redirects an administrator with a temporary password to the password change page', async () => {
    await appRouter.navigate('/products')

    render(
      <I18nProvider>
        <AuthContext.Provider value={{
          actor: {
            kind: 1,
            id: 'admin-1',
            organizationId: null,
            displayName: 'Admin',
            permissions: [],
            sessionId: 'session-1',
            requiresPasswordChange: true,
          },
          isLoading: false,
          passwordForRequiredChange: 'temporary-password',
          signIn: vi.fn(),
          changePassword: vi.fn(),
          switchEmployee: vi.fn(),
          signOut: vi.fn(),
          clearSession: vi.fn(),
        }}>
          <RouterProvider router={appRouter} />
        </AuthContext.Provider>
      </I18nProvider>,
    )

    expect(await screen.findByRole('heading', { name: 'Ustaw nowe hasło' })).toBeInTheDocument()
    expect(screen.getByLabelText('Aktualne hasło')).toHaveValue('temporary-password')
    expect(appRouter.state.location.pathname).toBe('/change-password')
  })
})
