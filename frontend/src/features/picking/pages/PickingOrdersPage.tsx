import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { getPickingOrders, type PickingFilters, type PickingOrderListItem } from '../api/pickingApi'
import { enumIs, formatDate, pickingLocale, pickingStatusClass, pickingStatusKey } from '../pickingFormat'

const pageSize = 20

export function PickingOrdersPage() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [customer, setCustomer] = useState('')
  const [status, setStatus] = useState('')
  const [dueDateFrom, setDueDateFrom] = useState('')
  const [dueDateTo, setDueDateTo] = useState('')
  const filters: PickingFilters = {
    page,
    pageSize,
    search: search.trim() || undefined,
    customer: customer.trim() || undefined,
    status: status === '' ? undefined : Number(status),
    dueDateFrom: dueDateFrom || undefined,
    dueDateTo: dueDateTo || undefined,
  }
  const query = useQuery({
    queryKey: ['picking', 'orders', filters],
    queryFn: () => getPickingOrders(filters),
    placeholderData: keepPreviousData,
    refetchOnMount: 'always',
  })
  const totalPages = Number(query.data?.totalPages ?? 0)
  const orders = query.data?.items ?? []

  function updateFilter(setter: (value: string) => void, value: string) {
    setPage(1)
    setter(value)
  }

  return <section className="mx-auto max-w-[1400px]" aria-labelledby="picking-orders-heading">
    <div className="mb-6">
      <h2 id="picking-orders-heading" className="text-2xl font-bold tracking-tight lg:text-3xl">{t('picking.list.title')}</h2>
      <p className="mt-1 text-sm text-slate-600">{query.isLoading
        ? t('picking.list.loading')
        : t('picking.list.count').replace('{count}', String(query.data?.totalCount ?? 0))}</p>
    </div>

    <div className="mb-5 grid gap-3 rounded-xl border border-slate-300 bg-white p-4 sm:grid-cols-2 xl:grid-cols-5">
      <label className="relative block sm:col-span-2">
        <span className="sr-only">{t('picking.list.search')}</span>
        <AppIcon className="pointer-events-none absolute left-3 top-1/2 size-5 -translate-y-1/2 text-slate-500" name="search" />
        <input className="h-11 w-full rounded-lg border border-slate-300 pl-10 pr-3" placeholder={t('picking.list.searchPlaceholder')} value={search} onChange={(e) => updateFilter(setSearch, e.target.value)} />
      </label>
      <label><span className="sr-only">{t('picking.list.status')}</span><select className="h-11 w-full rounded-lg border border-slate-300 px-3" value={status} onChange={(e) => updateFilter(setStatus, e.target.value)}><option value="">{t('picking.list.allStatuses')}</option><option value="0">{t('picking.status.waiting')}</option><option value="1">{t('picking.status.inProgress')}</option><option value="2">{t('picking.status.completed')}</option></select></label>
      <label><span className="sr-only">{t('picking.list.customer')}</span><input className="h-11 w-full rounded-lg border border-slate-300 px-3" placeholder={t('picking.list.customer')} value={customer} onChange={(e) => updateFilter(setCustomer, e.target.value)} /></label>
      <div className="grid grid-cols-2 gap-2"><label><span className="sr-only">{t('picking.list.dueFrom')}</span><input aria-label={t('picking.list.dueFrom')} className="h-11 w-full rounded-lg border border-slate-300 px-2 text-sm" type="date" value={dueDateFrom} onChange={(e) => updateFilter(setDueDateFrom, e.target.value)} /></label><label><span className="sr-only">{t('picking.list.dueTo')}</span><input aria-label={t('picking.list.dueTo')} className="h-11 w-full rounded-lg border border-slate-300 px-2 text-sm" type="date" value={dueDateTo} onChange={(e) => updateFilter(setDueDateTo, e.target.value)} /></label></div>
    </div>

    {query.isLoading ? <ListState text={t('picking.list.loading')} />
      : query.isError ? <ListState error text={t('picking.list.error')} retry={() => void query.refetch()} />
        : orders.length === 0 ? <ListState text={t('picking.list.empty')} />
          : <><div className="hidden overflow-hidden rounded-xl border border-slate-300 bg-white md:block"><table className="w-full text-left"><thead className="border-b border-slate-300 bg-slate-100 text-sm text-slate-600"><tr><th className="p-4">{t('picking.list.orderNumber')}</th><th className="p-4">{t('picking.list.customer')}</th><th className="p-4">{t('picking.list.dueDate')}</th><th className="p-4">{t('picking.list.status')}</th><th className="p-4">{t('picking.list.packingProgress')}</th><th className="p-4"><span className="sr-only">{t('picking.list.open')}</span></th></tr></thead><tbody className="divide-y divide-slate-200">{orders.map((order) => <OrderRow key={order.id} order={order} open={() => navigate(`/picking/${order.id}`)} />)}</tbody></table></div>
            <div className="grid gap-3 md:hidden">{orders.map((order) => <OrderCard key={order.id} order={order} open={() => navigate(`/picking/${order.id}`)} />)}</div></>}

    {totalPages > 1 && <nav aria-label={t('picking.pagination')} className="mt-5 flex items-center justify-between"><span className="text-sm text-slate-600">{t('picking.page').replace('{page}', String(page)).replace('{pages}', String(totalPages))}</span><div className="flex gap-2"><button aria-label={t('picking.previous')} className="admin-action-button" disabled={page <= 1 || query.isFetching} onClick={() => setPage((x) => x - 1)}><AppIcon name="chevronLeft" /></button><button aria-label={t('picking.next')} className="admin-action-button" disabled={page >= totalPages || query.isFetching} onClick={() => setPage((x) => x + 1)}><AppIcon name="chevronRight" /></button></div></nav>}
  </section>
}

function OrderRow({ order, open }: { order: PickingOrderListItem; open: () => void }) {
  const { language, t } = useI18n()
  const overdue = isOverdue(order)
  return <tr tabIndex={0} onClick={open} onKeyDown={(e) => e.key === 'Enter' && open()} className={`cursor-pointer hover:bg-slate-50 ${overdue ? 'bg-red-50/60' : ''}`}>
    <td className="p-4 font-semibold"><span className="inline-flex items-center gap-2">{order.number}{order.isAssignedToCurrentUser && <span title={t('picking.list.assigned')} aria-label={t('picking.list.assigned')} className="text-blue-900"><AppIcon className="size-5" name="personCheck" /></span>}{overdue && <span title={t('picking.list.overdue')} aria-label={t('picking.list.overdue')} className="text-red-700"><AppIcon className="size-5" name="warning" /></span>}</span></td>
    <td className="p-4">{order.customerName}</td><td className={`p-4 ${overdue ? 'font-semibold text-red-700' : ''}`}>{formatDate(order.dueDate, pickingLocale(language))}</td><td className="p-4"><Status value={order.pickingStatus} /></td><td className="p-4"><Progress order={order} /></td><td className="p-4 text-right"><AppIcon name="chevronRight" /></td>
  </tr>
}

function OrderCard({ order, open }: { order: PickingOrderListItem; open: () => void }) {
  const { language, t } = useI18n()
  const overdue = isOverdue(order)
  return <button onClick={open} className={`rounded-xl border p-4 text-left shadow-sm ${overdue ? 'border-red-300 bg-red-50' : 'border-slate-300 bg-white'}`}><div className="flex items-start justify-between gap-3"><div><strong className="inline-flex items-center gap-2">{order.number}{order.isAssignedToCurrentUser && <span aria-label={t('picking.list.assigned')} className="text-blue-900"><AppIcon className="size-5" name="personCheck" /></span>}</strong><p className="mt-1 text-sm text-slate-600">{order.customerName}</p></div><Status value={order.pickingStatus} /></div><div className="mt-4 grid grid-cols-2 gap-3 border-t border-slate-200 pt-3 text-sm"><div><span className="block text-slate-500">{t('picking.list.dueDate')}</span><span className={overdue ? 'font-semibold text-red-700' : ''}>{formatDate(order.dueDate, pickingLocale(language))} {overdue && `— ${t('picking.list.overdueSuffix')}`}</span></div><div><span className="block text-slate-500">{t('picking.list.progress')}</span>{order.completedItemCount}/{order.totalItemCount}</div></div><div className="mt-3"><Progress order={order} /></div></button>
}

function Status({ value }: { value: number | string }) { const { t } = useI18n(); return <span className={`inline-flex whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold ${pickingStatusClass(value)}`}>{t(pickingStatusKey(value))}</span> }
function Progress({ order }: { order: PickingOrderListItem }) { return <div className="flex min-w-32 items-center gap-3"><div className="h-2 flex-1 overflow-hidden rounded-full bg-slate-200"><span className={`block h-full ${enumIs(order.pickingStatus, 2, 'Completed') ? 'bg-emerald-700' : 'bg-blue-900'}`} style={{ width: `${order.progressPercent}%` }} /></div><span className="text-sm">{order.progressPercent}%</span></div> }
function isOverdue(order: PickingOrderListItem) { return order.dueDate < new Date().toISOString().slice(0, 10) && !enumIs(order.pickingStatus, 2, 'Completed') }
function ListState({ text, error, retry }: { text: string; error?: boolean; retry?: () => void }) { const { t } = useI18n(); return <div role={error ? 'alert' : 'status'} className={`grid min-h-64 place-items-center rounded-xl border p-6 text-center ${error ? 'border-red-300 bg-red-50 text-red-900' : 'border-slate-300 bg-white text-slate-600'}`}><div><p className="font-semibold">{text}</p>{retry && <button className="mt-4 rounded-lg bg-red-800 px-4 py-2 font-semibold text-white" onClick={retry}>{t('picking.retry')}</button>}</div></div> }
