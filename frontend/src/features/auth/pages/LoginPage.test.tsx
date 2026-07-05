import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { I18nProvider } from '../../../app/i18n/I18nProvider'
import { AuthApiError } from '../api/authApi'
import { AuthContext, type AuthContextValue } from '../authContext'
import { LoginPage } from './LoginPage'
import * as authApi from '../api/authApi'

vi.mock('../api/authApi', async (importOriginal) => {
  const original = await importOriginal<typeof authApi>()
  return {
    ...original,
    getOrganizations: vi.fn(),
    getEmployees: vi.fn(),
  }
})

afterEach(cleanup)

function renderLogin(
  signIn: AuthContextValue['signIn'] = vi.fn(),
  switchEmployee: AuthContextValue['switchEmployee'] = vi.fn(),
) {
  render(
    <I18nProvider>
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <AuthContext.Provider
          value={{ actor: null, isLoading: false, signIn, changePassword: vi.fn(), switchEmployee, signOut: vi.fn(), clearSession: vi.fn() }}
        >
          <MemoryRouter initialEntries={['/login']}>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/" element={<h1>Panel</h1>} />
            </Routes>
          </MemoryRouter>
        </AuthContext.Provider>
      </QueryClientProvider>
    </I18nProvider>,
  )
}

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.mocked(authApi.getOrganizations).mockReset()
    vi.mocked(authApi.getEmployees).mockReset()
  })

  it('changes the interface language to Spanish and persists the choice', async () => {
    const user = userEvent.setup()
    renderLogin()

    await user.selectOptions(screen.getByRole('combobox', { name: 'Język' }), 'es')

    expect(
      screen.getByText('Inicio de sesión en el sistema de almacén'),
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Usuario')).toBeInTheDocument()
    expect(document.documentElement.lang).toBe('es')
    expect(localStorage.getItem('subiekt-mobile-language')).toBe('es')
  })

  it('signs in and redirects to the requested application route', async () => {
    const user = userEvent.setup()
    const signIn = vi.fn().mockResolvedValue(undefined)
    renderLogin(signIn)

    await user.type(screen.getByLabelText('Użytkownik'), ' admin ')
    await user.type(screen.getByLabelText('Hasło'), 'secret-password')
    await user.click(screen.getByRole('button', { name: 'Zaloguj' }))

    expect(signIn).toHaveBeenCalledWith({
      username: 'admin',
      password: 'secret-password',
    })
    expect(await screen.findByRole('heading', { name: 'Panel' })).toBeInTheDocument()
  })

  it('shows a localized message for invalid credentials', async () => {
    const user = userEvent.setup()
    const signIn = vi.fn().mockRejectedValue(new AuthApiError(401))
    renderLogin(signIn)

    await user.type(screen.getByLabelText('Użytkownik'), 'admin')
    await user.type(screen.getByLabelText('Hasło'), 'wrong-password')
    await user.click(screen.getByRole('button', { name: 'Zaloguj' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Nieprawidłowy użytkownik lub hasło.',
    )
  })

  it('lets an employee select an organization and sign in', async () => {
    const user = userEvent.setup()
    const switchEmployee = vi.fn().mockResolvedValue(undefined)
    vi.mocked(authApi.getOrganizations).mockResolvedValue([
      { id: 'organization-1', code: 'MAG', name: 'Magazyn Centralny' },
    ])
    vi.mocked(authApi.getEmployees).mockResolvedValue([
      {
        id: 'employee-1',
        organizationId: 'organization-1',
        code: 'A1',
        displayName: 'Anna Nowak',
      },
    ])
    renderLogin(undefined, switchEmployee)

    await user.click(screen.getByRole('tab', { name: 'Pracownik' }))
    await user.selectOptions(
      await screen.findByRole('combobox', { name: 'Organizacja' }),
      'organization-1',
    )
    await user.selectOptions(
      await screen.findByRole('combobox', { name: 'Pracownik' }),
      'employee-1',
    )
    await user.click(screen.getByRole('button', { name: 'Wejdź jako pracownik' }))

    expect(switchEmployee).toHaveBeenCalledWith('organization-1', 'employee-1')
    expect(await screen.findByRole('heading', { name: 'Panel' })).toBeInTheDocument()
  })
})
