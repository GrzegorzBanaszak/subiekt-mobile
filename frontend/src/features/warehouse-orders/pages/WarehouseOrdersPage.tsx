import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { getWarehouseOrders, type WarehouseOrderListItem } from '../api/warehouseOrdersApi'
import { formatWarehouseOrderDate, isPublishedWarehouseOrder, warehouseOrderStatusKey } from '../warehouseOrderFormat'

const pageSize = 20

function statusClass(status: number | string) {
  return isPublishedWarehouseOrder(status) ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'
}

export function WarehouseOrdersPage() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const query = useQuery({
    queryKey: ['warehouse-orders', { page, pageSize }],
    queryFn: () => getWarehouseOrders(page, pageSize),
    placeholderData: keepPreviousData,
    refetchOnMount: 'always',
  })
  const orders = useMemo(() => {
    const phrase = search.trim().toLocaleLowerCase()
    if (!phrase) return query.data?.items ?? []
    return (query.data?.items ?? []).filter((warehouseOrder) =>
      `${warehouseOrder.number} ${warehouseOrder.customerName}`.toLocaleLowerCase().includes(phrase))
  }, [query.data?.items, search])
  const totalPages = Number(query.data?.totalPages ?? 0)

  return <section className="mx-auto max-w-[1400px]" aria-labelledby="orders-heading">
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div><h2 id="orders-heading" className="text-2xl font-bold tracking-tight lg:text-3xl">{t('orders.list.title')}</h2><p className="mt-1 text-sm text-slate-600">{query.isLoading ? t('orders.list.loading') : t('orders.list.count').replace('{count}', String(query.data?.totalCount ?? 0))}</p></div>
      <Link className="inline-flex min-h-12 items-center justify-center gap-2 rounded-lg bg-blue-950 px-5 font-semibold text-white hover:bg-blue-900" to="/warehouse-orders/new"><AppIcon className="size-5" name="add" />{t('orders.list.new')}</Link>
    </div>

    <div className="mb-5 flex flex-col gap-3 sm:flex-row"><label className="relative block w-full sm:max-w-sm"><span className="sr-only">{t('orders.list.search')}</span><AppIcon className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-500" name="search" /><input className="h-12 w-full rounded-lg border border-slate-300 bg-white pl-12 pr-4 outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20" placeholder={t('orders.list.searchPlaceholder')} value={search} onChange={(event) => setSearch(event.target.value)} /></label></div>

    {query.isLoading ? <OrderState text={t('orders.list.loading')} />
      : query.isError ? <OrderState error text={t('orders.list.error')} retry={() => void query.refetch()} />
      : orders.length === 0 ? <OrderState text={t('orders.list.empty')} />
      : <><div className="hidden overflow-hidden rounded-xl border border-slate-300 bg-white md:block"><table className="w-full text-left"><thead className="border-b border-slate-300 bg-slate-100 text-sm text-slate-600"><tr><th className="p-4">{t('orders.list.orderNumber')}</th><th className="p-4">{t('orders.customer')}</th><th className="p-4">{t('orders.dueDate')}</th><th className="p-4">{t('orders.status')}</th><th className="p-4 text-center">{t('orders.items')}</th><th className="p-4"><span className="sr-only">{t('orders.open')}</span></th></tr></thead><tbody className="divide-y divide-slate-200">{orders.map((warehouseOrder) => <WarehouseOrderRow key={warehouseOrder.id} warehouseOrder={warehouseOrder} open={() => navigate(`/warehouse-orders/${warehouseOrder.id}`)} />)}</tbody></table></div><div className="grid gap-3 md:hidden">{orders.map((warehouseOrder) => <WarehouseOrderCard key={warehouseOrder.id} warehouseOrder={warehouseOrder} open={() => navigate(`/warehouse-orders/${warehouseOrder.id}`)} />)}</div></>}

    {totalPages > 1 && <nav aria-label={t('orders.pagination')} className="mt-5 flex items-center justify-between"><span className="text-sm text-slate-600">{t('orders.page').replace('{page}', String(page)).replace('{pages}', String(totalPages))}</span><div className="flex gap-2"><button aria-label={t('orders.previous')} className="admin-action-button" disabled={page <= 1 || query.isFetching} onClick={() => setPage((x) => x - 1)}><AppIcon name="chevronLeft" /></button><button aria-label={t('orders.next')} className="admin-action-button" disabled={page >= totalPages || query.isFetching} onClick={() => setPage((x) => x + 1)}><AppIcon name="chevronRight" /></button></div></nav>}
  </section>
}

function WarehouseOrderRow({ warehouseOrder, open }: { warehouseOrder: WarehouseOrderListItem; open: () => void }) { const { language } = useI18n(); return <tr tabIndex={0} onClick={open} onKeyDown={(e) => e.key === 'Enter' && open()} className="cursor-pointer hover:bg-slate-50"><td className="p-4 font-semibold">{warehouseOrder.number}</td><td className="p-4">{warehouseOrder.customerName}</td><td className="p-4">{formatWarehouseOrderDate(warehouseOrder.dueDate, language)}</td><td className="p-4"><Status value={warehouseOrder.status} /></td><td className="p-4 text-center">{warehouseOrder.itemCount}</td><td className="p-4 text-right"><AppIcon name="chevronRight" /></td></tr> }
function WarehouseOrderCard({ warehouseOrder, open }: { warehouseOrder: WarehouseOrderListItem; open: () => void }) { const { language, t } = useI18n(); return <button onClick={open} className="rounded-xl border border-slate-300 bg-white p-4 text-left shadow-sm"><div className="flex items-start justify-between gap-3"><div><strong>{warehouseOrder.number}</strong><p className="mt-1 text-sm text-slate-600">{warehouseOrder.customerName}</p></div><Status value={warehouseOrder.status} /></div><div className="mt-4 grid grid-cols-2 gap-3 border-t border-slate-200 pt-3 text-sm"><div><span className="block text-slate-500">{t('orders.dueDate')}</span>{formatWarehouseOrderDate(warehouseOrder.dueDate, language)}</div><div><span className="block text-slate-500">{t('orders.items')}</span>{warehouseOrder.itemCount}</div></div></button> }
function Status({ value }: { value: number | string }) { const { t } = useI18n(); return <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${statusClass(value)}`}>{t(warehouseOrderStatusKey(value))}</span> }
function OrderState({ text, error, retry }: { text: string; error?: boolean; retry?: () => void }) { const { t } = useI18n(); return <div role={error ? 'alert' : 'status'} className={`grid min-h-64 place-items-center rounded-xl border p-6 text-center ${error ? 'border-red-300 bg-red-50 text-red-900' : 'border-slate-300 bg-white text-slate-600'}`}><div>{error && <AppIcon className="mx-auto mb-3 size-8" name="warning" />}<p className="font-semibold">{text}</p>{retry && <button className="mt-4 rounded-lg bg-red-800 px-4 py-2 font-semibold text-white" onClick={retry}>{t('orders.retry')}</button>}</div></div> }
