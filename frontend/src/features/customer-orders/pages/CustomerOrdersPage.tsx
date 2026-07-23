import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { getCustomerOrders } from '../api/customerOrdersApi'

const pageSize = 20

export function CustomerOrdersPage() {
  const { language, t } = useI18n()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [includeCompleted, setIncludeCompleted] = useState(false)
  const query = useQuery({
    queryKey: ['customer-orders', { page, search, includeCompleted }],
    queryFn: () => getCustomerOrders({ search: search || undefined, includeCompleted, page, pageSize }),
    placeholderData: keepPreviousData,
  })
  const date = new Intl.DateTimeFormat(language === 'pl' ? 'pl-PL' : 'es-ES')
  const totalPages = Number(query.data?.totalPages ?? 0)

  return <section className="mx-auto max-w-[1400px]" aria-labelledby="customer-orders-heading">
    <div className="mb-6"><h2 className="text-2xl font-bold tracking-tight lg:text-3xl" id="customer-orders-heading">{t('customerOrders.title')}</h2><p className="mt-1 text-sm text-slate-600">{t('customerOrders.sourceDescription')}</p></div>
    <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center"><label className="relative block w-full sm:max-w-sm"><span className="sr-only">{t('customerOrders.search')}</span><AppIcon className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-500" name="search" /><input className="h-12 w-full rounded-lg border border-slate-300 bg-white pl-12 pr-4" onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder={t('customerOrders.searchPlaceholder')} value={search} /></label><label className="flex min-h-12 items-center gap-2 text-sm font-semibold"><input checked={includeCompleted} onChange={(event) => { setIncludeCompleted(event.target.checked); setPage(1) }} type="checkbox" />{t('customerOrders.includeCompleted')}</label></div>
    {query.isLoading ? <State text={t('customerOrders.loading')} /> : query.isError ? <State error retry={() => void query.refetch()} text={t('customerOrders.error')} /> : (query.data?.items.length ?? 0) === 0 ? <State text={t('customerOrders.empty')} /> : <><div className="hidden overflow-hidden rounded-xl border border-slate-300 bg-white md:block"><table className="w-full text-left"><thead className="border-b border-slate-300 bg-slate-100 text-sm text-slate-600"><tr><th className="p-4">{t('customerOrders.number')}</th><th className="p-4">{t('customerOrders.customer')}</th><th className="p-4">{t('customerOrders.deliveryDate')}</th><th className="p-4 text-center">{t('customerOrders.items')}</th><th className="p-4">{t('customerOrders.status')}</th><th className="p-4" /></tr></thead><tbody className="divide-y divide-slate-200">{query.data?.items.map((order) => <tr className="hover:bg-slate-50" key={order.sourceDocumentId}><td className="p-4 font-semibold">{order.number}</td><td className="p-4">{order.customerName}</td><td className="p-4">{date.format(new Date(`${order.requestedDeliveryDate}T00:00:00`))}</td><td className="p-4 text-center">{order.itemCount}</td><td className="p-4"><Status converted={Boolean(order.warehouseOrderId)} t={t} /></td><td className="p-4 text-right"><Link className="admin-action-button" to={`/customer-orders/${order.sourceDocumentId}`}>{t('customerOrders.open')}</Link></td></tr>)}</tbody></table></div><div className="grid gap-3 md:hidden">{query.data?.items.map((order) => <Link className="rounded-xl border border-slate-300 bg-white p-4" key={order.sourceDocumentId} to={`/customer-orders/${order.sourceDocumentId}`}><p className="font-semibold">{order.number}</p><p className="mt-1 text-sm text-slate-600">{order.customerName}</p><p className="mt-3 text-sm text-slate-600">{date.format(new Date(`${order.requestedDeliveryDate}T00:00:00`))} · {t('customerOrders.items')}: {order.itemCount}</p><div className="mt-3"><Status converted={Boolean(order.warehouseOrderId)} t={t} /></div></Link>)}</div></>}
    {totalPages > 1 && <nav aria-label={t('customerOrders.pagination')} className="mt-5 flex items-center justify-between"><span className="text-sm text-slate-600">{t('customerOrders.page').replace('{page}', String(page)).replace('{pages}', String(totalPages))}</span><div className="flex gap-2"><button className="admin-action-button" disabled={page <= 1 || query.isFetching} onClick={() => setPage((value) => value - 1)}>{t('customerOrders.previous')}</button><button className="admin-action-button" disabled={page >= totalPages || query.isFetching} onClick={() => setPage((value) => value + 1)}>{t('customerOrders.next')}</button></div></nav>}
  </section>
}

function Status({ converted, t }: { converted: boolean; t: (key: 'customerOrders.converted' | 'customerOrders.available') => string }) { return <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${converted ? 'bg-emerald-100 text-emerald-800' : 'bg-blue-100 text-blue-950'}`}>{t(converted ? 'customerOrders.converted' : 'customerOrders.available')}</span> }
function State({ text, error, retry }: { text: string; error?: boolean; retry?: () => void }) { return <div className={`rounded-xl border p-8 text-center ${error ? 'border-red-300 bg-red-50 text-red-900' : 'border-slate-300 bg-white text-slate-600'}`} role={error ? 'alert' : 'status'}><p>{text}</p>{retry && <button className="mt-4 font-semibold underline" onClick={retry}>↻</button>}</div> }
