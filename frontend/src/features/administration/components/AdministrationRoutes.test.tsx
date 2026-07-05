import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext } from '../../auth/authContext'
import { I18nProvider } from '../../../app/i18n/I18nProvider'
import { AdministrationGuard, AdministrationLayout } from './AdministrationRoutes'
import { administratorsManagePermission } from '../permissions'

afterEach(cleanup)

function renderGuard(permissions: string[]) {
  render(
    <AuthContext.Provider value={{
      actor: { kind: 1, id: 'admin-1', organizationId: null, displayName: 'Admin', permissions, sessionId: 'session-1' },
      isLoading: false, signIn: vi.fn(), switchEmployee: vi.fn(), signOut: vi.fn(), clearSession: vi.fn(),
    }}>
      <MemoryRouter initialEntries={['/administration/administrators']}>
        <Routes>
          <Route path="/products" element={<h1>Towary</h1>} />
          <Route path="/administration/administrators" element={
            <AdministrationGuard permission={administratorsManagePermission}>
              <h1>Administratorzy</h1>
            </AdministrationGuard>
          } />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('AdministrationGuard', () => {
  it('blocks regular administrator from administrator management route', () => {
    renderGuard(['identity.manage'])
    expect(screen.getByRole('heading', { name: 'Towary' })).toBeInTheDocument()
  })

  it('allows bootstrap administrator to enter administrator management route', () => {
    renderGuard(['identity.manage', 'identity.administrators.manage'])
    expect(screen.getByRole('heading', { name: 'Administratorzy' })).toBeInTheDocument()
  })
})

function renderLayout(permissions: string[]) {
  render(
    <I18nProvider>
      <AuthContext.Provider value={{
        actor: { kind: 1, id: 'admin-1', organizationId: null, displayName: 'Admin', permissions, sessionId: 'session-1' },
        isLoading: false, signIn: vi.fn(), switchEmployee: vi.fn(), signOut: vi.fn(), clearSession: vi.fn(),
      }}>
        <MemoryRouter initialEntries={['/administration/organizations']}>
          <Routes>
            <Route path="/administration" element={<AdministrationLayout />}>
              <Route path="organizations" element={<p>Lista organizacji</p>} />
            </Route>
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>
    </I18nProvider>,
  )
}

describe('AdministrationLayout', () => {
  it('hides administrator tab from regular administrator', () => {
    renderLayout(['identity.manage'])
    expect(screen.queryByRole('link', { name: 'Administratorzy' })).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Organizacje' })).toBeInTheDocument()
  })

  it('shows administrator tab to bootstrap administrator', () => {
    renderLayout(['identity.manage', administratorsManagePermission])
    expect(screen.getByRole('link', { name: 'Administratorzy' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Organizacje' })).toBeInTheDocument()
  })
})
