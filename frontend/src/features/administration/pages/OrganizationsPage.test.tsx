import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { I18nProvider } from '../../../app/i18n/I18nProvider'
import * as administrationApi from '../api/administrationApi'
import { OrganizationsPage } from './OrganizationsPage'

vi.mock('../api/administrationApi', async (importOriginal) => {
  const original = await importOriginal<typeof administrationApi>()
  return { ...original, getOrganizations: vi.fn(), createOrganization: vi.fn() }
})

afterEach(cleanup)

describe('OrganizationsPage', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.mocked(administrationApi.getOrganizations).mockReset().mockResolvedValue([])
    vi.mocked(administrationApi.createOrganization).mockReset().mockResolvedValue({
      id: 'org-1', code: 'MAG', name: 'Magazyn Główny', isActive: true,
      createdAtUtc: '2026-07-05T12:00:00Z', updatedAtUtc: '2026-07-05T12:00:00Z',
    })
  })

  it('creates an organization and refreshes the list', async () => {
    const user = userEvent.setup()
    render(
      <I18nProvider>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <MemoryRouter><OrganizationsPage /></MemoryRouter>
        </QueryClientProvider>
      </I18nProvider>,
    )

    await screen.findByText('Nie znaleziono organizacji.')
    await user.click(screen.getByRole('button', { name: /Dodaj organizację/i }))
    await user.type(screen.getByLabelText('Kod'), 'MAG')
    await user.type(screen.getByLabelText('Nazwa organizacji'), 'Magazyn Główny')
    await user.click(screen.getByRole('button', { name: 'Zapisz' }))

    await waitFor(() =>
      expect(vi.mocked(administrationApi.createOrganization).mock.calls[0]?.[0]).toEqual({
        code: 'MAG',
        name: 'Magazyn Główny',
      }),
    )
    expect(administrationApi.getOrganizations).toHaveBeenCalledTimes(2)
  })
})
