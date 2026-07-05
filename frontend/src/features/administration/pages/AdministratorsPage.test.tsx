import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { I18nProvider } from '../../../app/i18n/I18nProvider'
import { AuthContext } from '../../auth/authContext'
import * as administrationApi from '../api/administrationApi'
import { AdministratorsPage } from './AdministratorsPage'

vi.mock('../api/administrationApi', async (importOriginal) => {
  const original = await importOriginal<typeof administrationApi>()
  return {
    ...original,
    getAdministrators: vi.fn(),
    resetAdministratorPassword: vi.fn(),
  }
})

afterEach(cleanup)

describe('AdministratorsPage', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.mocked(administrationApi.getAdministrators).mockReset().mockResolvedValue([
      {
        id: 'root-1', username: 'root', displayName: 'Root', isActive: true,
        isBootstrapAdministrator: true, createdAtUtc: '2026-07-05T12:00:00Z', updatedAtUtc: '2026-07-05T12:00:00Z',
      },
    ])
    vi.mocked(administrationApi.resetAdministratorPassword).mockReset().mockResolvedValue(undefined)
  })

  it('clears current root session after resetting its own password', async () => {
    const user = userEvent.setup()
    const clearSession = vi.fn()
    render(
      <I18nProvider>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <AuthContext.Provider value={{
            actor: {
              kind: 1, id: 'root-1', organizationId: null, displayName: 'Root',
              permissions: ['identity.manage', 'identity.administrators.manage'], sessionId: 'session-1',
            },
            isLoading: false, signIn: vi.fn(), switchEmployee: vi.fn(), signOut: vi.fn(), clearSession,
          }}>
            <MemoryRouter initialEntries={['/administration']}>
              <Routes>
                <Route path="/administration" element={<AdministratorsPage />} />
                <Route path="/login" element={<h1>Logowanie</h1>} />
              </Routes>
            </MemoryRouter>
          </AuthContext.Provider>
        </QueryClientProvider>
      </I18nProvider>,
    )

    await screen.findByText('Root')
    await user.click(screen.getByRole('button', { name: /Resetuj hasło/i }))
    await user.type(screen.getByLabelText('Hasło'), 'new-secure-password')
    await user.type(screen.getByLabelText('Powtórz hasło'), 'new-secure-password')
    await user.click(screen.getByRole('button', { name: 'Zapisz hasło' }))

    await waitFor(() => expect(clearSession).toHaveBeenCalledOnce())
    expect(screen.getByRole('heading', { name: 'Logowanie' })).toBeInTheDocument()
  })
})
