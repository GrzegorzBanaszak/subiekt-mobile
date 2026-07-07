import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { I18nProvider } from '../../../app/i18n/I18nProvider'
import { getProductDetails } from '../../products/api/productsApi'
import { ProductDetailsDialog } from './ProductDetailsDialog'

vi.mock('../../products/api/productsApi', () => ({ getProductDetails: vi.fn() }))
const mockedGetProductDetails = vi.mocked(getProductDetails)

afterEach(cleanup)

describe('ProductDetailsDialog', () => {
  beforeEach(() => {
    localStorage.clear()
    mockedGetProductDetails.mockReset()
    mockedGetProductDetails.mockResolvedValue({
      id: 42,
      name: 'Wkręt konstrukcyjny',
      symbol: 'WK-100',
      description: 'Stal ocynkowana',
      unit: 'szt.',
      unitWeightKg: 0.2,
      primaryBarcode: null,
      additionalBarcodes: [],
      vat: null,
      imageUrl: '/api/products/42/image',
      warehouses: [{
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
      }],
      prices: [],
    })
  })

  it('loads and displays the requested product information', async () => {
    const close = vi.fn()
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    render(<I18nProvider><QueryClientProvider client={queryClient}><ProductDetailsDialog fallbackName="Produkt" onClose={close} productId={42} /></QueryClientProvider></I18nProvider>)

    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    expect(await screen.findAllByText('Wkręt konstrukcyjny')).not.toHaveLength(0)
    expect(screen.getByText('WK-100')).toBeVisible()
    expect(screen.getByText('Stal ocynkowana')).toBeVisible()
    expect(screen.getByText('18 szt.')).toBeVisible()
    expect(mockedGetProductDetails).toHaveBeenCalledWith(42)

    await userEvent.click(screen.getByRole('button', { name: 'Powiększ zdjęcie produktu' }))
    expect(screen.getByRole('dialog', { name: 'Zdjęcie produktu na pełnym ekranie' })).toBeVisible()
    await userEvent.click(screen.getByRole('button', { name: 'Zamknij zdjęcie pełnoekranowe' }))
    expect(screen.queryByRole('dialog', { name: 'Zdjęcie produktu na pełnym ekranie' })).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Zamknij informacje o produkcie' }))
    expect(close).toHaveBeenCalledOnce()
  })
})
