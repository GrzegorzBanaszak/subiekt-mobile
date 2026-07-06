import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { I18nProvider } from '../../../app/i18n/I18nProvider'
import { getProducts } from '../api/productsApi'
import { ProductsPage } from './ProductsPage'

vi.mock('../api/productsApi', async (importOriginal) => {
  const original = await importOriginal<typeof import('../api/productsApi')>()
  return { ...original, getProducts: vi.fn() }
})

const mockedGetProducts = vi.mocked(getProducts)

afterEach(cleanup)

function renderProducts() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  render(
    <I18nProvider>
      <QueryClientProvider client={queryClient}>
        <ProductsPage />
      </QueryClientProvider>
    </I18nProvider>,
  )
}

describe('ProductsPage', () => {
  beforeEach(() => {
    localStorage.clear()
    mockedGetProducts.mockReset()
    mockedGetProducts.mockResolvedValue({
      items: [
        {
          id: 42,
          name: 'Wkręt konstrukcyjny',
          symbol: 'WK-100',
          unit: 'szt.',
          unitWeightKg: 0.2,
          imageUrl: null,
          stock: {
            warehouseId: 1,
            warehouseSymbol: 'MAG',
            warehouseName: 'Magazyn główny',
            isMain: true,
            quantity: 18,
            reserved: 3,
            available: 15,
            minimum: null,
            maximum: null,
            unit: 'szt.',
          },
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    })
  })

  it('renders product data returned by the catalog API', async () => {
    renderProducts()

    expect(await screen.findAllByText('Wkręt konstrukcyjny')).not.toHaveLength(0)
    expect(screen.getAllByText('WK-100')).not.toHaveLength(0)
    expect(screen.getAllByText('15')).not.toHaveLength(0)
    expect(mockedGetProducts).toHaveBeenCalledWith({ search: '', page: 1, pageSize: 20 })
  })

  it('passes a trimmed search term after the debounce interval', async () => {
    const user = userEvent.setup()
    renderProducts()

    await screen.findAllByText('Wkręt konstrukcyjny')
    await user.type(screen.getByRole('searchbox'), '  WK-100  ')

    await waitFor(() =>
      expect(mockedGetProducts).toHaveBeenLastCalledWith({
        search: 'WK-100',
        page: 1,
        pageSize: 20,
      }),
    )
  })
})
