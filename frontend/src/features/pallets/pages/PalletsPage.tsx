import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { getPallets, type PalletListItem } from '../api/palletsApi'
import { formatWeightKg, palletLocale, palletStatusClass, palletStatusKey } from '../palletFormat'

const pageSize = 20

export function PalletsPage() {
  const { language, t } = useI18n()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const query = useQuery({
    queryKey: ['pallets', 'list', page, pageSize],
    queryFn: () => getPallets(page, pageSize),
    placeholderData: keepPreviousData,
    refetchOnMount: 'always',
  })
  const pallets = query.data?.items ?? []
  const totalPages = Number(query.data?.totalPages ?? 0)
  const locale = palletLocale(language)

  return <section className="mx-auto max-w-[1400px]" aria-labelledby="pallets-heading">
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h2 className="text-2xl font-bold tracking-tight lg:text-3xl" id="pallets-heading">{t('pallets.list.title')}</h2>
        <p className="mt-1 text-sm text-slate-600">{query.isLoading
          ? t('pallets.list.loading')
          : t('pallets.list.count').replace('{count}', String(query.data?.totalCount ?? 0))}</p>
      </div>
      <button className="admin-action-button" disabled={query.isFetching} onClick={() => void query.refetch()} type="button">
        <AppIcon className={query.isFetching ? 'size-5 animate-spin' : 'size-5'} name="refresh" />
        {t('picking.detail.refresh')}
      </button>
    </div>

    {query.isLoading ? <ListState text={t('pallets.list.loading')} />
      : query.isError ? <ListState error retry={() => void query.refetch()} text={t('pallets.list.error')} />
        : pallets.length === 0 ? <ListState text={t('pallets.list.empty')} />
          : <>
            <div className="hidden overflow-hidden rounded-xl border border-slate-300 bg-white md:block">
              <table className="w-full text-left">
                <thead className="border-b border-slate-300 bg-slate-100 text-sm text-slate-600">
                  <tr>
                    <th className="p-4">{t('pallets.list.palletNumber')}</th>
                    <th className="p-4">{t('pallets.list.orderNumber')}</th>
                    <th className="p-4">{t('pallets.list.customer')}</th>
                    <th className="p-4">{t('pallets.list.status')}</th>
                    <th className="p-4 text-right">{t('pallets.list.weight')}</th>
                    <th className="p-4 text-center">{t('pallets.list.items')}</th>
                    <th className="p-4">{t('pallets.list.closedAt')}</th>
                    <th className="p-4"><span className="sr-only">{t('pallets.list.open')}</span></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {pallets.map((pallet) => <PalletRow key={pallet.id} locale={locale} open={() => navigate(`/pallets/${pallet.id}`)} pallet={pallet} />)}
                </tbody>
              </table>
            </div>
            <div className="grid gap-3 md:hidden">
              {pallets.map((pallet) => <PalletCard key={pallet.id} locale={locale} open={() => navigate(`/pallets/${pallet.id}`)} pallet={pallet} />)}
            </div>
          </>}

    {totalPages > 1 && <nav aria-label={t('orders.pagination')} className="mt-5 flex items-center justify-between">
      <span className="text-sm text-slate-600">{t('orders.page').replace('{page}', String(page)).replace('{pages}', String(totalPages))}</span>
      <div className="flex gap-2">
        <button aria-label={t('orders.previous')} className="admin-action-button" disabled={page <= 1 || query.isFetching} onClick={() => setPage((x) => x - 1)} type="button"><AppIcon name="chevronLeft" /></button>
        <button aria-label={t('orders.next')} className="admin-action-button" disabled={page >= totalPages || query.isFetching} onClick={() => setPage((x) => x + 1)} type="button"><AppIcon name="chevronRight" /></button>
      </div>
    </nav>}
  </section>
}

function PalletRow({ pallet, locale, open }: { pallet: PalletListItem; locale: string; open: () => void }) {
  const { t } = useI18n()
  return <tr className="cursor-pointer hover:bg-slate-50" onClick={open} onKeyDown={(event) => event.key === 'Enter' && open()} tabIndex={0}>
    <td className="p-4 font-semibold"><span className="inline-flex items-center gap-2"><AppIcon className="size-5 text-indigo-800" name="pallet" />{pallet.palletNumber}</span></td>
    <td className="p-4">{pallet.orderNumber}</td>
    <td className="p-4">{pallet.customerName}</td>
    <td className="p-4"><Status value={pallet.status} /></td>
    <td className="p-4 text-right font-semibold">{formatWeightKg(Number(pallet.totalWeightKg), locale)}</td>
    <td className="p-4 text-center">{pallet.itemCount}</td>
    <td className="p-4">{formatDateTime(pallet.closedAtUtc, locale)}</td>
    <td className="p-4 text-right"><span aria-label={t('pallets.list.open')}><AppIcon name="chevronRight" /></span></td>
  </tr>
}

function PalletCard({ pallet, locale, open }: { pallet: PalletListItem; locale: string; open: () => void }) {
  const { t } = useI18n()
  return <button className="rounded-xl border border-slate-300 bg-white p-4 text-left shadow-sm" onClick={open} type="button">
    <div className="flex items-start justify-between gap-3">
      <div>
        <strong className="inline-flex items-center gap-2 text-blue-950"><AppIcon className="size-5 text-indigo-800" name="pallet" />{pallet.palletNumber}</strong>
        <p className="mt-1 text-sm text-slate-600">{pallet.customerName}</p>
      </div>
      <Status value={pallet.status} />
    </div>
    <div className="mt-4 grid grid-cols-2 gap-3 border-t border-slate-200 pt-3 text-sm">
      <div><span className="block text-slate-500">{t('pallets.list.orderNumber')}</span>{pallet.orderNumber}</div>
      <div><span className="block text-slate-500">{t('pallets.list.weight')}</span>{formatWeightKg(Number(pallet.totalWeightKg), locale)}</div>
      <div><span className="block text-slate-500">{t('pallets.list.items')}</span>{pallet.itemCount}</div>
      <div><span className="block text-slate-500">{t('pallets.list.closedAt')}</span>{formatDateTime(pallet.closedAtUtc, locale)}</div>
    </div>
  </button>
}

function Status({ value }: { value: number | string }) {
  const { t } = useI18n()
  return <span className={`inline-flex whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold ${palletStatusClass(value)}`}>{t(palletStatusKey(value))}</span>
}

function formatDateTime(value: string, locale: string) {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function ListState({ text, error, retry }: { text: string; error?: boolean; retry?: () => void }) {
  const { t } = useI18n()
  return <div className={`grid min-h-64 place-items-center rounded-xl border p-6 text-center ${error ? 'border-red-300 bg-red-50 text-red-900' : 'border-slate-300 bg-white text-slate-600'}`} role={error ? 'alert' : 'status'}>
    <div>
      <AppIcon className="mx-auto mb-3 size-8" name={error ? 'warning' : 'pallet'} />
      <p className="font-semibold">{text}</p>
      {retry && <button className="mt-4 rounded-lg bg-red-800 px-4 py-2 font-semibold text-white" onClick={retry} type="button">{t('orders.retry')}</button>}
    </div>
  </div>
}
