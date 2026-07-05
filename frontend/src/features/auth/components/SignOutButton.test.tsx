import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { I18nProvider } from '../../../app/i18n/I18nProvider'
import { AuthContext } from '../authContext'
import { SignOutButton } from './SignOutButton'

afterEach(cleanup)

describe('SignOutButton', () => {
  it('signs out the current user', async () => {
    const user = userEvent.setup()
    const signOut = vi.fn().mockResolvedValue(undefined)

    render(
      <I18nProvider>
        <AuthContext.Provider value={{
          actor: null,
          isLoading: false,
          signIn: vi.fn(),
          switchEmployee: vi.fn(),
          signOut,
          clearSession: vi.fn(),
        }}>
          <SignOutButton />
        </AuthContext.Provider>
      </I18nProvider>,
    )

    await user.click(screen.getByRole('button', { name: 'Wyloguj' }))

    expect(signOut).toHaveBeenCalledOnce()
  })
})
