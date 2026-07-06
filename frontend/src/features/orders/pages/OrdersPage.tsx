import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AppIcon } from '../../../shared/components/AppIcon'
import { getOrders, type OrderListItem } from '../api/ordersApi'

const pageSize = 20

function isPublished(status: number | string) {
  return status === 1 || status === 'ReadyForPicking'
}

function statusLabel(status: number | string) {
  return isPublished(status) ? 'Gotowe do kompletacji' : 'Wersja robocza'
}

function statusClass(status: number | string) {
  return isPublished(status)
    ? 'bg-emerald-100 text-emerald-800'
    : 'bg-slate-200 text-slate-700'
}

export function OrdersPage() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const query = useQuery({
    queryKey: ['orders', { page, pageSize }],
    queryFn: () => getOrders(page, pageSize),
    placeholderData: keepPreviousData,
    refetchOnMount: 'always',
  })
  const orders = useMemo(() => {
    const phrase = search.trim().toLocaleLowerCase('pl')
    if (!phrase) return query.data?.items ?? []
    return (query.data?.items ?? []).filter((order) =>
      `${order.number} ${order.customerName}`.toLocaleLowerCase('pl').includes(phrase))
  }, [query.data?.items, search])
  const totalPages = Number(query.data?.totalPages ?? 0)

  return (
    <section className="mx-auto max-w-[1400px]" aria-labelledby="orders-heading">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 id="orders-heading" className="text-2xl font-bold tracking-tight lg:text-3xl">Lista zamówień</h2>
          <p className="mt-1 text-sm text-slate-600">{query.isLoading ? 'Ładowanie…' : `Liczba zamówień: ${query.data?.totalCount ?? 0}`}</p>
        </div>
        <Link className="inline-flex min-h-12 items-center justify-center gap-2 rounded-lg bg-blue-950 px-5 font-semibold text-white hover:bg-blue-900" to="/orders/new">
          <AppIcon className="size-5" name="add" /> Nowe zamówienie
        </Link>
      </div>

      <div className="mb-5 flex flex-col gap-3 sm:flex-row">
        <label className="relative block w-full sm:max-w-sm">
          <span className="sr-only">Szukaj zamówienia</span>
          <AppIcon className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-500" name="search" />
          <input className="h-12 w-full rounded-lg border border-slate-300 bg-white pl-12 pr-4 outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20" placeholder="Szukaj na tej stronie…" value={search} onChange={(event) => setSearch(event.target.value)} />
        </label>
      </div>

      {query.isLoading ? <OrderState text="Ładowanie zamówień…" />
        : query.isError ? <OrderState error text="Nie udało się pobrać zamówień." retry={() => void query.refetch()} />
        : orders.length === 0 ? <OrderState text="Brak zamówień spełniających kryteria." />
        : <>
          <div className="hidden overflow-hidden rounded-xl border border-slate-300 bg-white md:block">
            <table className="w-full text-left">
              <thead className="border-b border-slate-300 bg-slate-100 text-sm text-slate-600"><tr>
                <th className="p-4">Nr zamówienia</th><th className="p-4">Klient</th><th className="p-4">Termin</th><th className="p-4">Status</th><th className="p-4 text-center">Pozycje</th><th className="p-4"><span className="sr-only">Otwórz</span></th>
              </tr></thead>
              <tbody className="divide-y divide-slate-200">{orders.map((order) =>
                <OrderRow key={order.id} order={order} open={() => navigate(`/orders/${order.id}`)} />)}</tbody>
            </table>
          </div>
          <div className="grid gap-3 md:hidden">{orders.map((order) =>
            <button key={order.id} onClick={() => navigate(`/orders/${order.id}`)} className="rounded-xl border border-slate-300 bg-white p-4 text-left shadow-sm">
              <div className="flex items-start justify-between gap-3"><div><strong>{order.number}</strong><p className="mt-1 text-sm text-slate-600">{order.customerName}</p></div><Status value={order.status} /></div>
              <div className="mt-4 grid grid-cols-2 gap-3 border-t border-slate-200 pt-3 text-sm"><div><span className="block text-slate-500">Termin</span>{formatDate(order.dueDate)}</div><div><span className="block text-slate-500">Pozycje</span>{order.itemCount}</div></div>
            </button>)}</div>
        </>}

      {totalPages > 1 && <nav aria-label="Stronicowanie zamówień" className="mt-5 flex items-center justify-between">
        <span className="text-sm text-slate-600">Strona {page} z {totalPages}</span><div className="flex gap-2">
          <button className="admin-action-button" disabled={page <= 1 || query.isFetching} onClick={() => setPage((x) => x - 1)}><AppIcon name="chevronLeft" /><span className="sr-only">Poprzednia</span></button>
          <button className="admin-action-button" disabled={page >= totalPages || query.isFetching} onClick={() => setPage((x) => x + 1)}><AppIcon name="chevronRight" /><span className="sr-only">Następna</span></button>
        </div></nav>}
    </section>
  )
}

function OrderRow({ order, open }: { order: OrderListItem; open: () => void }) {
  return <tr tabIndex={0} onClick={open} onKeyDown={(e) => e.key === 'Enter' && open()} className="cursor-pointer hover:bg-slate-50">
    <td className="p-4 font-semibold">{order.number}</td><td className="p-4">{order.customerName}</td><td className="p-4">{formatDate(order.dueDate)}</td><td className="p-4"><Status value={order.status} /></td><td className="p-4 text-center">{order.itemCount}</td><td className="p-4 text-right"><AppIcon name="chevronRight" /></td>
  </tr>
}

function Status({ value }: { value: number | string }) { return <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${statusClass(value)}`}>{statusLabel(value)}</span> }
function formatDate(value: string) { return new Intl.DateTimeFormat('pl-PL').format(new Date(`${value}T00:00:00`)) }
function OrderState({ text, error, retry }: { text: string; error?: boolean; retry?: () => void }) { return <div role={error ? 'alert' : 'status'} className={`grid min-h-64 place-items-center rounded-xl border p-6 text-center ${error ? 'border-red-300 bg-red-50 text-red-900' : 'border-slate-300 bg-white text-slate-600'}`}><div>{error && <AppIcon className="mx-auto mb-3 size-8" name="warning" />}<p className="font-semibold">{text}</p>{retry && <button className="mt-4 rounded-lg bg-red-800 px-4 py-2 font-semibold text-white" onClick={retry}>Spróbuj ponownie</button>}</div></div> }
