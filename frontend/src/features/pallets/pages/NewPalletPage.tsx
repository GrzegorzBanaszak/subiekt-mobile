import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiRequestError } from '../../../api/apiError'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import {
  createPallet,
  getPalletCandidates,
  type CreatePalletItem,
  type PalletCandidateItem,
} from '../api/palletsApi'
import { formatPalletQuantity, formatWeightKg, palletLocale } from '../palletFormat'

interface CreateVariables {
  emptyPalletWeightKg: number
  items: CreatePalletItem[]
}

const emptyPalletCandidates: PalletCandidateItem[] = []

export function NewPalletPage() {
  const { language, t } = useI18n()
  const locale = palletLocale(language)
  const { warehouseOrderId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState<Record<string, boolean>>({})
  const [quantities, setQuantities] = useState<Record<string, string>>({})
  const [emptyPalletWeight, setEmptyPalletWeight] = useState('25.0')
  const [message, setMessage] = useState<string | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)

  const query = useQuery({
    queryKey: ['pallets', 'candidates', warehouseOrderId],
    queryFn: () => getPalletCandidates(warehouseOrderId),
    enabled: Boolean(warehouseOrderId),
    refetchOnMount: 'always',
  })

  const mutation = useMutation({
    mutationFn: (variables: CreateVariables) =>
      createPallet(warehouseOrderId, variables.emptyPalletWeightKg, variables.items),
    onMutate: () => setMessage(null),
    onSuccess: (pallet) => {
      void queryClient.invalidateQueries({ queryKey: ['pallets', 'candidates', warehouseOrderId] })
      void queryClient.invalidateQueries({ queryKey: ['picking', 'order', warehouseOrderId] })
      void queryClient.invalidateQueries({ queryKey: ['picking', 'orders'] })
      setConfirmOpen(false)
      navigate(`/pallets/${pallet.id}`)
    },
    onError: (error) => {
      setConfirmOpen(false)
      if (error instanceof ApiRequestError && error.status === 409) {
        setMessage(t('pallets.create.message.conflict'))
        void query.refetch()
        return
      }
      setMessage(error instanceof ApiRequestError && error.status === 400
        ? t('pallets.create.message.validation')
        : t('pallets.create.message.error'))
    },
  })

  const candidates = query.data?.items ?? emptyPalletCandidates
  const selectedItems = useMemo(
    () => candidates.filter((item) => selected[item.warehouseOrderItemId]),
    [candidates, selected],
  )
  const goodsWeight = selectedItems.reduce((sum, item) => {
    const quantity = quantityFor(item)
    const unitWeight = Number(item.unitWeightKg ?? 0)
    return sum + (Number.isFinite(quantity) && Number.isFinite(unitWeight) ? quantity * unitWeight : 0)
  }, 0)
  const tare = Number(emptyPalletWeight)
  const totalWeight = goodsWeight + (Number.isFinite(tare) ? tare : 0)

  function quantityFor(item: PalletCandidateItem) {
    return Number(quantities[item.warehouseOrderItemId] ?? item.availableForPalletQuantity)
  }

  function quantityText(item: PalletCandidateItem) {
    return quantities[item.warehouseOrderItemId] ?? String(item.availableForPalletQuantity)
  }

  function toggleItem(item: PalletCandidateItem, checked: boolean) {
    setSelected((current) => ({ ...current, [item.warehouseOrderItemId]: checked }))
    setQuantities((current) => ({
      ...current,
      [item.warehouseOrderItemId]: current[item.warehouseOrderItemId] ?? String(item.availableForPalletQuantity),
    }))
  }

  function selectAll() {
    const nextSelected: Record<string, boolean> = {}
    const nextQuantities = { ...quantities }
    for (const item of candidates) {
      if (Number(item.availableForPalletQuantity) > 0 && Number(item.unitWeightKg ?? 0) > 0) {
        nextSelected[item.warehouseOrderItemId] = true
        nextQuantities[item.warehouseOrderItemId] = nextQuantities[item.warehouseOrderItemId] ?? String(item.availableForPalletQuantity)
      }
    }
    setSelected(nextSelected)
    setQuantities(nextQuantities)
  }

  function clearSelection() {
    setSelected({})
  }

  function validate() {
    if (!Number.isFinite(tare) || tare < 0) return t('pallets.create.validation.tare')
    if (selectedItems.length === 0) return t('pallets.create.validation.selectItem')
    for (const item of selectedItems) {
      const quantity = quantityFor(item)
      if (!Number.isFinite(quantity) || quantity <= 0 || quantity > Number(item.availableForPalletQuantity)) {
        return t('pallets.create.validation.quantity')
      }
      if (Number(item.unitWeightKg ?? 0) <= 0) return t('pallets.create.validation.weight')
    }
    return null
  }

  function openConfirm() {
    const validation = validate()
    if (validation) {
      setMessage(validation)
      return
    }
    setMessage(null)
    setConfirmOpen(true)
  }

  function confirmCreate() {
    const validation = validate()
    if (validation) {
      setMessage(validation)
      setConfirmOpen(false)
      return
    }
    mutation.mutate({
      emptyPalletWeightKg: tare,
      items: selectedItems.map((item) => ({
        warehouseOrderItemId: item.warehouseOrderItemId,
        quantity: quantityFor(item),
        itemVersion: Number(item.version),
      })),
    })
  }

  if (query.isLoading) return <PageState text={t('pallets.create.loading')} />
  if (query.isError || !query.data) {
    return <PageState error retry={() => void query.refetch()} text={t('pallets.create.error')} />
  }

  return <section className="mx-auto max-w-[1400px]" aria-labelledby="new-pallet-heading">
    <header className="mb-5 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="flex items-start gap-3">
        <Link aria-label={t('pallets.create.back')} className="grid size-11 shrink-0 place-items-center rounded-full hover:bg-slate-200" to={`/picking/${warehouseOrderId}`}><AppIcon name="arrowBack" /></Link>
        <div>
          <p className="text-sm font-semibold text-slate-500">{t('pallets.create.eyebrow')}</p>
          <h2 className="text-2xl font-bold tracking-tight text-blue-950 sm:text-3xl" id="new-pallet-heading">{t('pallets.create.title')}</h2>
          <p className="mt-1 text-slate-600">{query.data.warehouseOrderNumber} · {query.data.customerName}</p>
        </div>
      </div>
      <div className="flex flex-wrap gap-2">
        <button className="admin-action-button" onClick={() => void query.refetch()} type="button">
          <AppIcon className={query.isFetching ? 'size-5 animate-spin' : 'size-5'} name="refresh" />
          {t('picking.detail.refresh')}
        </button>
      </div>
    </header>

    {message && <div className="mb-4 rounded-xl border border-amber-300 bg-amber-50 p-3 text-sm font-semibold text-amber-950" role="alert">{message}</div>}

    <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
      <section className="overflow-hidden rounded-xl border border-slate-300 bg-white" aria-labelledby="pallet-items-heading">
        <header className="flex flex-col gap-3 border-b border-slate-300 bg-slate-100 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h3 className="font-bold text-blue-950" id="pallet-items-heading">{t('pallets.create.itemsTitle')}</h3>
            <p className="text-sm text-slate-600">{t('pallets.create.subtitle')}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button className="admin-action-button" onClick={selectAll} type="button">{t('pallets.create.selectAll')}</button>
            <button className="admin-action-button" onClick={clearSelection} type="button">{t('pallets.create.clearSelection')}</button>
          </div>
        </header>

        {candidates.length === 0
          ? <div className="grid min-h-56 place-items-center p-6 text-center text-slate-600"><div><AppIcon className="mx-auto mb-3 size-8" name="pallet" /><p className="font-semibold">{t('pallets.create.empty')}</p></div></div>
          : <div className="divide-y divide-slate-200">
            {candidates.map((item) => {
              const unitWeight = Number(item.unitWeightKg ?? 0)
              const unavailable = Number(item.availableForPalletQuantity) <= 0 || unitWeight <= 0
              const checked = Boolean(selected[item.warehouseOrderItemId])
              return <article className={`grid gap-4 p-4 md:grid-cols-[auto_1fr_auto] md:items-center ${checked ? 'bg-emerald-50' : 'bg-white'}`} key={item.warehouseOrderItemId}>
                <label className="flex items-center gap-3 font-semibold text-blue-950">
                  <input checked={checked} className="size-5" disabled={unavailable} onChange={(event) => toggleItem(item, event.target.checked)} type="checkbox" />
                  <span className="sr-only">{item.productName}</span>
                </label>
                <div className="min-w-0">
                  <h4 className="font-bold text-blue-950">{item.productName}</h4>
                  <p className="mt-1 text-sm text-slate-500">{item.productSymbol || `ID: ${item.productId}`}</p>
                  <div className="mt-3 flex flex-wrap gap-2 text-sm">
                    <span className="rounded-lg bg-slate-100 px-3 py-1.5">{t('pallets.create.available')}: <strong>{formatPalletQuantity(Number(item.availableForPalletQuantity), locale)} {item.unit}</strong></span>
                    <span className="rounded-lg bg-slate-100 px-3 py-1.5">{t('orders.unitWeight')}: <strong>{unitWeight > 0 ? formatWeightKg(unitWeight, locale) : t('orders.missing')}</strong></span>
                  </div>
                </div>
                <label className="block md:w-48">
                  <span className="mb-1 block text-xs font-semibold text-slate-600">{t('pallets.create.selectedQuantity')}</span>
                  <div className="flex h-11 overflow-hidden rounded-lg border border-slate-300 bg-white">
                    <input aria-label={`${t('pallets.create.selectedQuantity')} ${item.productName}`} className="w-full px-3 text-right" disabled={!checked} max={Number(item.availableForPalletQuantity)} min="0.0001" onChange={(event) => setQuantities((current) => ({ ...current, [item.warehouseOrderItemId]: event.target.value }))} step="0.0001" type="number" value={quantityText(item)} />
                    <span className="grid place-items-center border-l border-slate-300 bg-slate-50 px-3 text-sm">{item.unit}</span>
                  </div>
                </label>
              </article>
            })}
          </div>}
      </section>

      <aside className="h-fit rounded-xl border border-slate-300 bg-white shadow-sm">
        <div className="border-b border-slate-200 p-4">
          <h3 className="font-bold text-blue-950">{t('pallets.create.palletData')}</h3>
          <label className="mt-4 block">
            <span className="mb-1 block text-sm font-semibold text-slate-600">{t('pallets.create.emptyPalletWeight')}</span>
            <div className="flex h-12 overflow-hidden rounded-lg border border-slate-300 bg-white">
              <input className="w-full px-3 text-right text-lg font-semibold" min="0" onChange={(event) => setEmptyPalletWeight(event.target.value)} step="0.1" type="number" value={emptyPalletWeight} />
              <span className="grid place-items-center border-l border-slate-300 bg-slate-50 px-3 text-sm">kg</span>
            </div>
          </label>
          <p className="mt-2 text-sm text-slate-600">{t('pallets.create.emptyPalletHint')}</p>
        </div>
        <div className="space-y-3 p-4">
          <h3 className="font-bold text-blue-950">{t('pallets.create.summary')}</h3>
          <SummaryRow label={t('pallets.create.selectedCount')} value={String(selectedItems.length)} />
          <SummaryRow label={t('pallets.details.goodsWeight')} value={formatWeightKg(goodsWeight, locale)} />
          <SummaryRow label={t('pallets.details.emptyPalletWeight')} value={Number.isFinite(tare) ? formatWeightKg(tare, locale) : '—'} />
          <div className="border-t border-slate-200 pt-3">
            <SummaryRow strong label={t('pallets.details.totalWeight')} value={Number.isFinite(totalWeight) ? formatWeightKg(totalWeight, locale) : '—'} />
          </div>
        </div>
        <div className="border-t border-slate-200 p-4">
          <button className="flex min-h-12 w-full items-center justify-center gap-2 rounded-lg bg-blue-950 px-4 font-semibold text-white hover:bg-blue-900 disabled:cursor-wait disabled:opacity-60" disabled={mutation.isPending} onClick={openConfirm} type="button">
            <AppIcon className="size-5" name="check" />
            {mutation.isPending ? t('pallets.create.closing') : t('pallets.create.close')}
          </button>
        </div>
      </aside>
    </div>

    {confirmOpen && <ConfirmDialog busy={mutation.isPending} cancel={() => setConfirmOpen(false)} confirm={confirmCreate} />}
  </section>
}

function SummaryRow({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return <div className={`flex items-center justify-between gap-3 ${strong ? 'text-lg font-bold text-blue-950' : 'text-sm'}`}>
    <span className="text-slate-600">{label}</span>
    <span>{value}</span>
  </div>
}

function ConfirmDialog({ busy, cancel, confirm }: { busy: boolean; cancel: () => void; confirm: () => void }) {
  const { t } = useI18n()
  return <div aria-modal="true" className="fixed inset-0 z-50 grid place-items-center bg-slate-950/50 p-4" role="dialog">
    <section className="w-full max-w-md rounded-xl bg-white p-5 shadow-2xl" aria-labelledby="pallet-confirm-title">
      <div className="flex items-start gap-3">
        <span className="grid size-11 shrink-0 place-items-center rounded-full bg-emerald-100 text-emerald-800"><AppIcon name="pallet" /></span>
        <div>
          <h3 className="text-lg font-bold text-blue-950" id="pallet-confirm-title">{t('pallets.create.confirmTitle')}</h3>
          <p className="mt-2 text-slate-600">{t('pallets.create.confirmText')}</p>
        </div>
      </div>
      <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button className="admin-action-button" disabled={busy} onClick={cancel} type="button">{t('pallets.create.confirmCancel')}</button>
        <button className="min-h-11 rounded-lg bg-blue-950 px-4 font-semibold text-white hover:bg-blue-900 disabled:cursor-wait disabled:opacity-60" disabled={busy} onClick={confirm} type="button">{busy ? t('pallets.create.closing') : t('pallets.create.confirmSubmit')}</button>
      </div>
    </section>
  </div>
}

function PageState({ text, error, retry }: { text: string; error?: boolean; retry?: () => void }) {
  const { t } = useI18n()
  return <div role={error ? 'alert' : 'status'} className={`mx-auto grid min-h-64 max-w-3xl place-items-center rounded-xl border p-8 text-center ${error ? 'border-red-300 bg-red-50 text-red-900' : 'border-slate-300 bg-white text-slate-600'}`}>
    <div>
      <AppIcon className="mx-auto mb-3 size-8" name={error ? 'warning' : 'pallet'} />
      <p className="font-bold">{text}</p>
      {retry && <button className="mt-4 rounded-lg bg-blue-950 px-4 py-2 font-semibold text-white" onClick={retry} type="button">{t('picking.retry')}</button>}
    </div>
  </div>
}
