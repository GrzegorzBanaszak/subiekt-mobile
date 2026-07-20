import { useMutation, useQuery, useQueryClient, type UseQueryResult } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiRequestError } from '../../../api/apiError'
import { useI18n } from '../../../app/i18n/i18nContext'
import type { TranslationKey } from '../../../app/i18n/translations'
import { useAuth } from '../../auth/authContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import {
  getPickingHistory,
  getPickingWarehouseOrder,
  mutatePickingItem,
  type PickingHistoryItem,
  type PickingHistoryPage,
  type PickingItem,
  type PickingMutation,
  type PickingWarehouseOrderDetails,
} from '../api/pickingApi'
import { formatDate, formatDateTime, formatQuantity, pickingLocale } from '../pickingFormat'
import { ProductDetailsDialog } from '../components/ProductDetailsDialog'
import {
  filterPickingItems,
  isSharedPicking,
  pickingActionKey,
  pickingItemStatusClass,
  pickingItemStatusKey,
  type PickingTab,
} from '../pickingView'

const tabKeys: Record<PickingTab, TranslationKey> = {
  all: 'picking.tab.all',
  available: 'picking.tab.available',
  mine: 'picking.tab.mine',
  packed: 'picking.tab.packed',
}
const sharedTabs: PickingTab[] = ['all', 'available', 'mine', 'packed']

interface MutationVariables {
  item: PickingItem
  action: PickingMutation
  packedQuantity?: number
}

export function PickingOrderPage() {
  const { language, t } = useI18n()
  const { warehouseOrderId = '' } = useParams()
  const { actor } = useAuth()
  const queryClient = useQueryClient()
  const [activeTab, setActiveTab] = useState<PickingTab>('all')
  const [message, setMessage] = useState<string | null>(null)
  const [showHistory, setShowHistory] = useState(false)
  const [historyPage, setHistoryPage] = useState(1)
  const [packQuantities, setPackQuantities] = useState<Record<string, string>>({})
  const [productItem, setProductItem] = useState<PickingItem | null>(null)

  const orderQuery = useQuery({
    queryKey: ['picking', 'warehouse-order', warehouseOrderId],
    queryFn: () => getPickingWarehouseOrder(warehouseOrderId),
    enabled: Boolean(warehouseOrderId),
    refetchOnMount: 'always',
  })
  const historyQuery = useQuery({
    queryKey: ['picking', 'history', warehouseOrderId, historyPage],
    queryFn: () => getPickingHistory(warehouseOrderId, historyPage),
    enabled: Boolean(warehouseOrderId) && showHistory,
  })
  const mutation = useMutation({
    mutationFn: ({ item, action, packedQuantity }: MutationVariables) =>
      mutatePickingItem(warehouseOrderId, item.id, Number(item.version), action, packedQuantity),
    onMutate: () => setMessage(null),
    onSuccess: (updated, variables) => {
      queryClient.setQueryData(['picking', 'warehouse-order', warehouseOrderId], updated)
      void queryClient.invalidateQueries({ queryKey: ['picking', 'warehouse-orders'] })
      void queryClient.invalidateQueries({ queryKey: ['picking', 'history', warehouseOrderId] })
      setPackQuantities((current) => {
        const next = { ...current }
        delete next[variables.item.id]
        return next
      })
      setMessage(t('picking.message.saved'))
    },
    onError: (error) => {
      if (error instanceof ApiRequestError && error.status === 409) {
        setMessage(t('picking.message.conflict'))
        void orderQuery.refetch()
        return
      }
      setMessage(error instanceof ApiRequestError && error.status === 400
        ? t('picking.error.validation')
        : t('picking.error.save'))
    },
  })

  if (orderQuery.isLoading) return <PageState text={t('picking.detail.loading')} />
  if (orderQuery.isError || !orderQuery.data) return <PageState error text={t('picking.detail.error')} retry={() => void orderQuery.refetch()} />

  const order = orderQuery.data
  const shared = isSharedPicking(order.pickingMode)
  const visibleTabs = shared ? sharedTabs : ['all' as const]
  const selectedTab = shared ? activeTab : 'all'
  const items = filterPickingItems(order.items, selectedTab, actor?.id)
  const lastUpdated = orderQuery.dataUpdatedAt
    ? new Intl.DateTimeFormat(pickingLocale(language), { timeStyle: 'medium' }).format(orderQuery.dataUpdatedAt)
    : '—'

  function execute(item: PickingItem, action: PickingMutation) {
    if (action === 'undo-pack' && !window.confirm(t('picking.confirm.undo').replace('{name}', item.productName))) return
    const packedQuantity = action === 'pack'
      ? Number(packQuantities[item.id] ?? item.remainingQuantity)
      : undefined
    if (action === 'pack' && (!Number.isFinite(packedQuantity) || packedQuantity! <= 0)) {
      setMessage(t('picking.error.quantityPositive'))
      return
    }
    mutation.mutate({ item, action, packedQuantity })
  }

  return <section className="mx-auto max-w-[1400px]" aria-labelledby="picking-heading">
    <header className="mb-5 rounded-2xl border border-slate-300 bg-white p-4 shadow-sm sm:p-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="flex items-start gap-3">
          <Link aria-label={t('picking.detail.back')} className="grid size-11 shrink-0 place-items-center rounded-full hover:bg-slate-100" to="/picking"><AppIcon name="arrowBack" /></Link>
          <div><p className="text-sm font-semibold text-slate-500">{t('picking.detail.eyebrow')}</p><h2 id="picking-heading" className="text-2xl font-bold tracking-tight text-blue-950 sm:text-3xl">{order.number}</h2><p className="mt-1 text-slate-600">{order.customerName}</p></div>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {order.isAssignedToCurrentUser && <span className="inline-flex min-h-10 items-center gap-2 rounded-full bg-blue-100 px-3 text-sm font-semibold text-blue-950"><AppIcon className="size-5" name="personCheck" />{t('picking.list.assigned')}</span>}
          {order.canCreatePallet && <Link className="admin-action-button bg-blue-950 text-white hover:bg-blue-900" to={`/picking/${order.id}/pallets/new`}><AppIcon className="size-5" name="pallet" />{t('picking.detail.createPallet')}</Link>}
          <button className="admin-action-button" disabled={orderQuery.isFetching} onClick={() => void orderQuery.refetch()}><AppIcon className={orderQuery.isFetching ? 'size-5 animate-spin' : 'size-5'} name="refresh" />{t('picking.detail.refresh')}</button>
          <button className="admin-action-button" onClick={() => setShowHistory((value) => !value)}><AppIcon className="size-5" name="history" />{t('picking.detail.history')}</button>
        </div>
      </div>

      <div className="mt-5 grid gap-4 border-t border-slate-200 pt-5 sm:grid-cols-[1fr_auto] sm:items-end">
        <div><div className="mb-2 flex items-center justify-between text-sm"><span className="font-semibold">{t('picking.detail.progress')}</span><span>{t('picking.detail.itemCount').replace('{completed}', String(order.completedItemCount)).replace('{total}', String(order.totalItemCount))}</span></div><div className="h-3 overflow-hidden rounded-full bg-slate-200"><span className="block h-full rounded-full bg-emerald-600 transition-[width]" style={{ width: `${Number(order.progressPercent)}%` }} /></div></div>
        <div className="grid grid-cols-2 gap-x-6 gap-y-1 text-sm sm:text-right"><span className="text-slate-500">{t('picking.list.dueDate')}</span><strong>{formatDate(order.dueDate, pickingLocale(language))}</strong><span className="text-slate-500">{t('picking.detail.lastRefresh')}</span><strong>{lastUpdated}</strong></div>
      </div>
    </header>

    {!order.canExecutePicking && <div role="status" className="mb-4 flex gap-3 rounded-xl border border-amber-300 bg-amber-50 p-4 text-amber-950"><AppIcon className="size-5" name="warning" /><p>{t('picking.detail.readOnly')}</p></div>}
    {message && <div role="status" className={`mb-4 rounded-xl border p-3 text-sm font-semibold ${mutation.isError ? 'border-amber-300 bg-amber-50 text-amber-950' : 'border-emerald-300 bg-emerald-50 text-emerald-900'}`}>{message}</div>}

    <nav aria-label={t('picking.detail.filters')} className="mb-4 flex gap-2 overflow-x-auto rounded-xl border border-slate-300 bg-white p-2">
      {visibleTabs.map((tab) => {
        const count = filterPickingItems(order.items, tab, actor?.id).length
        const selected = selectedTab === tab
        const label = shared ? t(tabKeys[tab]) : t('picking.tab.products')
        return <button aria-current={selected ? 'page' : undefined} className={`min-h-11 shrink-0 rounded-lg px-4 text-sm font-semibold ${selected ? 'bg-blue-950 text-white' : 'text-slate-700 hover:bg-slate-100'}`} key={tab} onClick={() => setActiveTab(tab)}>{label} <span className={`ml-1 rounded-full px-2 py-0.5 text-xs ${selected ? 'bg-white/20' : 'bg-slate-200'}`}>{count}</span></button>
      })}
    </nav>

    {items.length === 0
      ? <div className="grid min-h-48 place-items-center rounded-xl border border-slate-300 bg-white p-6 text-center text-slate-600">{t('picking.tab.empty')}</div>
      : <div className="grid gap-3">{items.map((item) => <PickingItemCard busy={mutation.isPending} execute={execute} item={item} key={item.id} packQuantity={packQuantities[item.id] ?? String(item.remainingQuantity)} setPackQuantity={(value) => setPackQuantities((current) => ({ ...current, [item.id]: value }))} showProduct={() => setProductItem(item)} />)}</div>}

    {showHistory && <HistoryPanel close={() => setShowHistory(false)} order={order} page={historyPage} query={historyQuery} setPage={setHistoryPage} />}
    {productItem && <ProductDetailsDialog fallbackName={productItem.productName} onClose={() => setProductItem(null)} productId={Number(productItem.productId)} />}
  </section>
}

function PickingItemCard({ item, busy, execute, packQuantity, setPackQuantity, showProduct }: {
  item: PickingItem
  busy: boolean
  execute: (item: PickingItem, action: PickingMutation) => void
  packQuantity: string
  setPackQuantity: (value: string) => void
  showProduct: () => void
}) {
  const { language, t } = useI18n()
  const locale = pickingLocale(language)
  const actor = item.reservedBy ?? item.packedBy
  const palletizedQuantity = Number(item.palletizedQuantity ?? 0)
  const availableForPallet = Number(item.availableForPalletQuantity ?? 0)
  const palletAssignments = item.palletAssignments ?? []
  return <article className="rounded-xl border border-slate-300 bg-white p-4 shadow-sm sm:p-5">
    <div className="flex flex-col gap-4 xl:flex-row xl:items-center">
      <div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><h3 className="font-bold text-blue-950 sm:text-lg">{item.productName}</h3><span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${pickingItemStatusClass(item.status)}`}>{t(pickingItemStatusKey(item.status))}</span></div><p className="mt-1 text-sm text-slate-500">{item.productSymbol || `ID: ${item.productId}`}</p>{actor && <p className="mt-3 inline-flex items-center gap-2 text-sm text-slate-700"><AppIcon className="size-5" name="user" /><span><strong>{actor.displayName}</strong> · {formatDateTime(actor.atUtc, locale)}</span></p>}</div>
      <dl className="grid shrink-0 grid-cols-2 gap-2 text-sm sm:grid-cols-4 xl:w-[600px]"><Quantity icon="cart" label={t('picking.item.ordered')} value={`${formatQuantity(Number(item.orderedQuantity), locale)} ${item.unit}`} /><Quantity icon="box" label={t('picking.item.packedQuantity')} value={`${formatQuantity(Number(item.packedQuantity ?? 0), locale)} ${item.unit}`} /><Quantity highlighted={palletizedQuantity > 0} icon="pallet" label={t('picking.item.palletizedQuantity')} value={`${formatQuantity(palletizedQuantity, locale)} ${item.unit}`} /><Quantity icon="clipboard" label={t('picking.item.remainingQuantity')} value={`${formatQuantity(Number(item.remainingQuantity), locale)} ${item.unit}`} /></dl>
      <div className="flex min-w-64 flex-col gap-2 sm:flex-row sm:items-end xl:justify-end">
        {item.actions.canPack && <label className="block"><span className="mb-1 block text-xs font-semibold text-slate-600">{t('picking.item.quantityToPack')}</span><div className="flex h-11 overflow-hidden rounded-lg border border-slate-300 bg-white"><input aria-label={`${t('picking.item.quantityToPack')} ${item.productName}`} className="w-28 px-3 text-right" min="0.0001" max={Number(item.remainingQuantity)} step="0.0001" type="number" value={packQuantity} onChange={(event) => setPackQuantity(event.target.value)} /><span className="grid place-items-center border-l border-slate-300 bg-slate-50 px-3 text-sm">{item.unit}</span></div></label>}
        <div className="flex flex-wrap gap-2"><button className="inline-flex min-h-11 items-center gap-2 rounded-lg border border-slate-300 bg-white px-4 font-semibold text-slate-800 hover:bg-slate-100" onClick={showProduct}><AppIcon className="size-5" name="info" />{t('picking.product.open')}</button>{item.actions.canReserve && <ActionButton busy={busy} label={t('picking.action.reserve')} onClick={() => execute(item, 'reserve')} />}{item.actions.canPack && <ActionButton busy={busy} label={t('picking.action.pack')} primary onClick={() => execute(item, 'pack')} />}{item.actions.canRelease && <ActionButton busy={busy} label={t('picking.action.release')} onClick={() => execute(item, 'release')} />}{item.actions.canUndoPack && <ActionButton busy={busy} label={t('picking.action.undo')} onClick={() => execute(item, 'undo-pack')} />}</div>
      </div>
    </div>
    {(palletizedQuantity > 0 || availableForPallet > 0) && <div className="mt-4 grid gap-3 border-t border-slate-200 pt-4 lg:grid-cols-[1fr_auto] lg:items-start">
      {palletizedQuantity > 0 && <section className="rounded-lg border border-indigo-200 bg-indigo-50 p-3">
        <h4 className="flex items-center gap-2 text-sm font-bold text-indigo-950"><AppIcon className="size-5" name="pallet" />{t('picking.item.palletAssignments')}</h4>
        <ul className="mt-2 flex flex-wrap gap-2 text-sm">
          {palletAssignments.map((assignment) => <li key={assignment.palletId}>
            <Link className="inline-flex min-h-8 items-center gap-1 rounded-full bg-white px-3 py-1 font-semibold text-indigo-950 ring-1 ring-indigo-200 hover:bg-indigo-100 focus:outline-none focus:ring-2 focus:ring-indigo-800" to={`/pallets/${assignment.palletId}`}>
              <AppIcon className="size-4" name="pallet" />
              {t('picking.item.palletAssignmentText').replace('{number}', assignment.palletNumber).replace('{quantity}', `${formatQuantity(Number(assignment.quantity), locale)} ${item.unit}`)}
            </Link>
          </li>)}
        </ul>
      </section>}
      {availableForPallet > 0 && <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-sm font-semibold text-emerald-950">
        {t('picking.item.availableForPallet')}: {formatQuantity(availableForPallet, locale)} {item.unit}
      </div>}
    </div>}
  </article>
}

function Quantity({ label, value, highlighted, icon }: { label: string; value: string; highlighted?: boolean; icon: 'cart' | 'box' | 'pallet' | 'clipboard' }) { return <div className={`rounded-lg p-3 ${highlighted ? 'bg-indigo-100 text-indigo-950' : 'bg-slate-100'}`}><dt className="flex items-center gap-1 text-xs text-slate-500"><AppIcon className="size-4" name={icon} />{label}</dt><dd className="mt-1 font-bold">{value}</dd></div> }
function ActionButton({ label, onClick, busy, primary }: { label: string; onClick: () => void; busy: boolean; primary?: boolean }) { return <button className={`min-h-11 rounded-lg px-4 font-semibold disabled:opacity-50 ${primary ? 'bg-emerald-700 text-white hover:bg-emerald-800' : 'border border-blue-950 bg-white text-blue-950 hover:bg-blue-50'}`} disabled={busy} onClick={onClick}>{label}</button> }

function HistoryPanel({ order, close, page, setPage, query }: {
  order: PickingWarehouseOrderDetails
  close: () => void
  page: number
  setPage: (page: number) => void
  query: UseQueryResult<PickingHistoryPage, Error>
}) {
  const { t } = useI18n()
  const totalPages = Number(query.data?.totalPages ?? 0)
  return <section aria-labelledby="history-heading" className="mt-5 overflow-hidden rounded-xl border border-slate-300 bg-white"><header className="flex items-center justify-between border-b border-slate-300 bg-slate-100 p-4"><div><h3 className="font-bold" id="history-heading">{t('picking.history.title')}</h3><p className="text-sm text-slate-600">{order.number}</p></div><button aria-label={t('picking.history.close')} className="admin-action-button" onClick={close}><AppIcon className="size-5" name="close" /></button></header>{query.isLoading ? <div className="p-6 text-center text-slate-600">{t('picking.history.loading')}</div> : query.isError ? <div className="p-6 text-center text-red-800">{t('picking.history.error')}</div> : query.data?.items.length === 0 ? <div className="p-6 text-center text-slate-600">{t('picking.history.empty')}</div> : <ol className="divide-y divide-slate-200">{query.data?.items.map((entry) => <HistoryEntry entry={entry} key={entry.id} />)}</ol>}{totalPages > 1 && <nav aria-label={t('picking.history.pagination')} className="flex items-center justify-between border-t border-slate-200 p-4"><span className="text-sm text-slate-600">{t('picking.page').replace('{page}', String(page)).replace('{pages}', String(totalPages))}</span><div className="flex gap-2"><button aria-label={t('picking.previous')} className="admin-action-button" disabled={page <= 1 || query.isFetching} onClick={() => setPage(page - 1)}><AppIcon name="chevronLeft" /></button><button aria-label={t('picking.next')} className="admin-action-button" disabled={page >= totalPages || query.isFetching} onClick={() => setPage(page + 1)}><AppIcon name="chevronRight" /></button></div></nav>}</section>
}

function HistoryEntry({ entry }: { entry: PickingHistoryItem }) { const { language, t } = useI18n(); const locale = pickingLocale(language); return <li className="grid gap-1 p-4 sm:grid-cols-[1fr_auto]"><div><strong>{t(pickingActionKey(entry.action))}</strong><p className="text-sm text-slate-600">{entry.productName}{entry.packedQuantity == null ? '' : ` · ${formatQuantity(Number(entry.packedQuantity), locale)}`}</p></div><p className="text-sm sm:text-right"><strong className="block">{entry.actorDisplayName}</strong><span className="text-slate-500">{formatDateTime(entry.occurredAtUtc, locale)}</span></p></li> }

function PageState({ text, error, retry }: { text: string; error?: boolean; retry?: () => void }) { const { t } = useI18n(); return <div role={error ? 'alert' : 'status'} className={`mx-auto grid min-h-64 max-w-3xl place-items-center rounded-xl border p-8 text-center ${error ? 'border-red-300 bg-red-50 text-red-900' : 'border-slate-300 bg-white text-slate-600'}`}><div><AppIcon className="mx-auto mb-3 size-8" name={error ? 'warning' : 'clipboard'} /><p className="font-bold">{text}</p>{retry && <button className="mt-4 rounded-lg bg-blue-950 px-4 py-2 font-semibold text-white" onClick={retry}>{t('picking.retry')}</button>}</div></div> }
