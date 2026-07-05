import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { I18nProvider } from '../../../app/i18n/I18nProvider'
import { AuthApiError } from '../api/authApi'
import { AuthContext, type AuthContextValue } from '../authContext'
import { LoginPage } from './LoginPage'

function renderLogin(signIn: AuthContextValue['signIn'] = vi.fn()) {
  render(
    <I18nProvider>
      <AuthContext.Provider
        value={{ actor: null, isLoading: false, signIn }}
      >
        <MemoryRouter initialEntries={['/login']}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/" element={<h1>Panel</h1>} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>
    </I18nProvider>,
  )
}

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear()
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
})
