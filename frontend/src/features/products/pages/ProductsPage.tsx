import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { getProducts } from '../api/productsApi'
import { ProductList } from '../components/ProductList'

const pageSize = 20

function numericValue(value: number | string) {
  return Number(value)
}

export function ProductsPage() {
  const { t } = useI18n()
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setPage(1)
    }, 300)

    return () => window.clearTimeout(timeout)
  }, [searchInput])

  const productsQuery = useQuery({
    queryKey: ['products', { search, page, pageSize }],
    queryFn: () => getProducts({ search, page, pageSize }),
    placeholderData: keepPreviousData,
  })

  const products = productsQuery.data?.items ?? []
  const totalCount = numericValue(productsQuery.data?.totalCount ?? 0)
  const totalPages = numericValue(productsQuery.data?.totalPages ?? 0)
  const currentPage = numericValue(productsQuery.data?.page ?? page)

  return (
    <section aria-labelledby="products-heading" className="mx-auto max-w-[1400px]">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight lg:text-3xl" id="products-heading">
            {t('products.title')}
          </h2>
          <p className="mt-1 text-sm text-slate-600">
            {productsQuery.isLoading
              ? t('products.loading')
              : t('products.count').replace('{count}', String(totalCount))}
          </p>
        </div>

        <label className="relative block w-full sm:max-w-sm">
          <span className="sr-only">{t('products.search')}</span>
          <AppIcon
            className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-500"
            name="search"
          />
          <input
            className="h-12 w-full rounded-lg border border-slate-300 bg-white pl-12 pr-4 outline-none transition placeholder:text-slate-500 focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20"
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder={t('products.searchPlaceholder')}
            type="search"
            value={searchInput}
          />
        </label>
      </div>

      {productsQuery.isLoading ? (
        <div className="grid min-h-64 place-items-center rounded-xl border border-slate-300 bg-white text-slate-600" role="status">
          {t('products.loading')}
        </div>
      ) : productsQuery.isError ? (
        <div className="flex min-h-64 flex-col items-center justify-center rounded-xl border border-red-300 bg-red-50 p-6 text-center" role="alert">
          <AppIcon className="mb-3 size-8 text-red-700" name="warning" />
          <h2 className="font-semibold text-red-950">{t('products.errorTitle')}</h2>
          <p className="mt-1 text-sm text-red-800">{t('products.errorDescription')}</p>
          <button
            className="mt-5 min-h-11 rounded-lg bg-red-800 px-5 font-semibold text-white hover:bg-red-900"
            onClick={() => void productsQuery.refetch()}
            type="button"
          >
            {t('products.retry')}
          </button>
        </div>
      ) : products.length === 0 ? (
        <div className="grid min-h-64 place-items-center rounded-xl border border-slate-300 bg-white p-6 text-center text-slate-600">
          <div>
            <AppIcon className="mx-auto mb-3 size-9 text-slate-400" name="box" />
            <p className="font-semibold text-slate-900">{t('products.emptyTitle')}</p>
            <p className="mt-1 text-sm">{t('products.emptyDescription')}</p>
          </div>
        </div>
      ) : (
        <ProductList products={products} />
      )}

      {totalPages > 1 && (
        <nav aria-label={t('products.pagination')} className="mt-5 flex items-center justify-between gap-4">
          <p className="text-sm text-slate-600">
            {t('products.page')
              .replace('{page}', String(currentPage))
              .replace('{pages}', String(totalPages))}
          </p>
          <div className="flex gap-2">
            <button
              aria-label={t('products.previous')}
              className="flex size-11 items-center justify-center rounded-lg border border-slate-300 bg-white text-slate-800 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40"
              disabled={currentPage <= 1 || productsQuery.isFetching}
              onClick={() => setPage((value) => Math.max(1, value - 1))}
              type="button"
            >
              <AppIcon name="chevronLeft" />
            </button>
            <button
              aria-label={t('products.next')}
              className="flex size-11 items-center justify-center rounded-lg border border-slate-300 bg-white text-slate-800 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40"
              disabled={currentPage >= totalPages || productsQuery.isFetching}
              onClick={() => setPage((value) => value + 1)}
              type="button"
            >
              <AppIcon name="chevronRight" />
            </button>
          </div>
        </nav>
      )}
    </section>
  )
}
