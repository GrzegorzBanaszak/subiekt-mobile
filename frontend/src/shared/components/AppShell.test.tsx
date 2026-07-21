import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { I18nProvider } from '../../app/i18n/I18nProvider'
import { AuthContext } from '../../features/auth/authContext'
import { AppShell } from './AppShell'

afterEach(cleanup)

const actor = {
  kind: 1,
  id: 'admin-1',
  organizationId: null,
  displayName: 'Admin',
  permissions: ['warehouse-orders.manage', 'warehouse-orders.read-published', 'pallets.manage', 'identity.manage', 'customers.manage'],
  sessionId: 'session-1',
  requiresPasswordChange: false,
}

function renderAppShell() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })

  return render(
    <I18nProvider>
      <QueryClientProvider client={queryClient}>
        <AuthContext.Provider value={{
          actor,
          isLoading: false,
          passwordForRequiredChange: null,
          signIn: vi.fn(),
          changePassword: vi.fn(),
          switchEmployee: vi.fn(),
          signOut: vi.fn(),
          clearSession: vi.fn(),
        }}>
          <MemoryRouter initialEntries={['/warehouse-orders']}>
            <Routes>
              <Route element={<AppShell />}>
                <Route path="/warehouse-orders" element={<p>Warehouse orders</p>} />
              </Route>
            </Routes>
          </MemoryRouter>
        </AuthContext.Provider>
      </QueryClientProvider>
    </I18nProvider>,
  )
}

describe('AppShell', () => {
  it('links the available customers module and leaves later VDA modules planned', async () => {
    const user = userEvent.setup()
    renderAppShell()

    screen.getAllByRole('link', { name: 'Zamówienia magazynowe' }).forEach((link) => {
      expect(link).toHaveAttribute('href', '/warehouse-orders')
    })
    screen.getAllByRole('link', { name: 'Klienci' }).forEach((link) => {
      expect(link).toHaveAttribute('href', '/customers')
    })
    expect(screen.getByTestId('planned-navigation-navigation.customerOrders')).toHaveTextContent('Zamówienia klientów')
    expect(screen.getByTestId('planned-navigation-navigation.shipments')).toHaveTextContent('Wysyłki')

    await user.click(screen.getByRole('button', { name: 'Więcej' }))

    const moreMenu = screen.getByLabelText('Więcej')
    expect(within(moreMenu).getByRole('link', { name: 'Klienci' })).toHaveAttribute('href', '/customers')
    expect(within(moreMenu).getByRole('link', { name: 'Administracja' })).toHaveAttribute('href', '/administration')
  })
})
