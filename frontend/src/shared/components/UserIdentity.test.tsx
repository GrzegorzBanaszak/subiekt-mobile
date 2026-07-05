import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { I18nProvider } from '../../app/i18n/I18nProvider'
import { AuthContext } from '../../features/auth/authContext'
import * as authApi from '../../features/auth/api/authApi'
import { UserIdentity } from './UserIdentity'

vi.mock('../../features/auth/api/authApi', async (importOriginal) => {
  const original = await importOriginal<typeof authApi>()
  return {
    ...original,
    getOrganizations: vi.fn(),
    getEmployees: vi.fn(),
  }
})

afterEach(cleanup)

describe('UserIdentity', () => {
  it('shows the organization and lets the user change the current employee', async () => {
    const user = userEvent.setup()
    const switchEmployee = vi.fn().mockResolvedValue(undefined)
    vi.mocked(authApi.getOrganizations).mockResolvedValue([
      { id: 'org-1', code: 'MAG', name: 'Magazyn Centralny' },
    ])
    vi.mocked(authApi.getEmployees).mockResolvedValue([
      { id: 'employee-1', organizationId: 'org-1', code: 'A1', displayName: 'Anna Nowak' },
      { id: 'employee-2', organizationId: 'org-1', code: 'J1', displayName: 'Jan Kowalski' },
    ])

    render(
      <I18nProvider>
        <QueryClientProvider client={new QueryClient()}>
          <AuthContext.Provider
            value={{
              actor: {
                kind: 2,
                id: 'employee-1',
                organizationId: 'org-1',
                displayName: 'Anna Nowak',
                permissions: ['catalog.read'],
                sessionId: 'session-1',
                requiresPasswordChange: false,
              },
              isLoading: false,
              signIn: vi.fn(),
              changePassword: vi.fn(),
              switchEmployee,
              signOut: vi.fn(),
              clearSession: vi.fn(),
            }}
          >
            <UserIdentity />
          </AuthContext.Provider>
        </QueryClientProvider>
      </I18nProvider>,
    )

    expect(await screen.findByText('Magazyn Centralny')).toBeInTheDocument()
    expect(screen.getByText('Zmień pracownika')).toBeVisible()
    await user.selectOptions(
      await screen.findByRole('combobox', { name: 'Aktualny pracownik' }),
      'employee-2',
    )

    expect(switchEmployee).toHaveBeenCalledWith('org-1', 'employee-2')
  })
})
