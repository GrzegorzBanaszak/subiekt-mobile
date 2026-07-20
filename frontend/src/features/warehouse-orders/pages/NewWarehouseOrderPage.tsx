import { useMutation, useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { createWarehouseOrder, getAvailableWarehouseOrderAssignees, publishWarehouseOrder, searchProducts, type ProductListItem } from '../api/warehouseOrdersApi'
import { formatWarehouseOrderNumber } from '../warehouseOrderFormat'

interface DraftItem { product: ProductListItem; quantity: number }

export function NewWarehouseOrderPage() {
  const { language, t } = useI18n()
  const navigate = useNavigate()
  const [customerName, setCustomerName] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [productSearch, setProductSearch] = useState('')
  const [items, setItems] = useState<DraftItem[]>([])
  const [pickingMode, setPickingMode] = useState(0)
  const [employeeIds, setEmployeeIds] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)
  const products = useQuery({ queryKey: ['warehouse-order-product-search', productSearch], queryFn: () => searchProducts(productSearch.trim()), enabled: productSearch.trim().length >= 2 })
  const workforce = useQuery({ queryKey: ['warehouse-order-assignees'], queryFn: getAvailableWarehouseOrderAssignees })
  const availableProducts = (products.data ?? []).filter((product) => !items.some((item) => item.product.id === product.id))
  const totalQuantity = useMemo(() => items.reduce((sum, item) => sum + item.quantity, 0), [items])
  const missingWeightCount = useMemo(() => items.filter((item) => item.product.unitWeightKg == null).length, [items])
  const totalWeight = useMemo(() => items.reduce((sum, item) => sum + Number(item.product.unitWeightKg ?? 0) * item.quantity, 0), [items])

  const save = useMutation({
    mutationFn: async (publish: boolean) => {
      if (!customerName.trim() || !dueDate || items.length === 0) throw new Error(t('orders.new.validation.required'))
      if (items.some((item) => !Number.isFinite(item.quantity) || item.quantity <= 0)) throw new Error(t('orders.new.validation.quantity'))
      if ((pickingMode === 0 && employeeIds.length !== 1) || (pickingMode === 1 && employeeIds.length === 0))
        throw new Error(t(pickingMode === 0 ? 'orders.new.validation.singleAssignee' : 'orders.new.validation.sharedAssignee'))
      if (publish && dueDate < new Date().toISOString().slice(0, 10)) throw new Error(t('orders.new.validation.pastDue'))
      let warehouseOrder = await createWarehouseOrder({
        customerName: customerName.trim(), dueDate, pickingMode, employeeIds,
        items: items.map((item) => ({ productId: Number(item.product.id), quantity: item.quantity })),
      })
      if (publish) warehouseOrder = await publishWarehouseOrder(warehouseOrder.id, Number(warehouseOrder.version))
      return warehouseOrder
    },
    onSuccess: (warehouseOrder) => navigate(`/warehouse-orders/${warehouseOrder.id}`),
    onError: (reason) => setError(reason instanceof Error ? reason.message : t('orders.new.saveError')),
  })

  function addProduct(product: ProductListItem) { setItems((current) => [...current, { product, quantity: 1 }]); setProductSearch('') }
  function setQuantity(id: number, quantity: number) { setItems((current) => current.map((item) => item.product.id === id ? { ...item, quantity } : item)) }
  function selectEmployee(employeeId: string) {
    setEmployeeIds((current) => pickingMode === 0 ? [employeeId] : current.includes(employeeId) ? current.filter((id) => id !== employeeId) : [...current, employeeId])
  }
  const formatWeight = (value: number) => `${formatWarehouseOrderNumber(value, language)} kg`

  return <section className="mx-auto max-w-[1200px]">
    <div className="mb-6 flex items-center gap-3"><Link aria-label={t('orders.details.back')} to="/warehouse-orders" className="flex size-11 items-center justify-center rounded-full hover:bg-slate-200"><AppIcon name="arrowBack" /></Link><div><h2 className="text-2xl font-bold">{t('orders.new.title')}</h2><p className="text-sm text-slate-600">{t('orders.status.draft')}</p></div></div>
    {error && <div role="alert" className="mb-5 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900">{error}</div>}
    <div className="grid gap-5 lg:grid-cols-12">
      <section className="rounded-xl border border-slate-300 bg-white p-5 lg:col-span-8"><h3 className="mb-5 border-b border-slate-200 pb-3 text-lg font-semibold text-blue-950">{t('orders.new.basicData')}</h3><div className="grid gap-4 md:grid-cols-2"><label className="text-sm font-semibold text-slate-700">{t('orders.customer')}<input className="mt-2 h-12 w-full rounded-lg border border-slate-300 px-4 font-normal outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20" value={customerName} onChange={(e) => setCustomerName(e.target.value)} placeholder={t('orders.new.customerPlaceholder')} /></label><label className="text-sm font-semibold text-slate-700">{t('orders.details.dueDate')}<input className="mt-2 h-12 w-full rounded-lg border border-slate-300 px-4 font-normal outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20" value={dueDate} onChange={(e) => setDueDate(e.target.value)} type="date" /></label></div></section>
      <aside className="rounded-xl border border-slate-300 bg-slate-100 p-5 lg:col-span-4"><h3 className="text-lg font-semibold">{t('orders.summary')}</h3><dl className="mt-4 grid gap-3"><Summary label={t('orders.items')} value={String(items.length)} /><Summary label={t('orders.totalQuantity')} value={formatWarehouseOrderNumber(totalQuantity, language)} /><Summary label={t('orders.estimatedWeight')} value={items.length === 0 ? '—' : missingWeightCount ? `${formatWeight(totalWeight)} (${t('orders.incompleteData')})` : formatWeight(totalWeight)} /></dl>{missingWeightCount > 0 && <p className="mt-3 text-sm text-amber-800">{t('orders.missingWeight').replace('{count}', String(missingWeightCount))}</p>}</aside>
      <section className="rounded-xl border border-slate-300 bg-white p-5 lg:col-span-12"><h3 className="mb-4 border-b border-slate-200 pb-3 text-lg font-semibold text-blue-950">{t('orders.new.assignment')}</h3><div className="mb-4 flex flex-col gap-3 sm:flex-row"><label className={`cursor-pointer rounded-lg border p-4 sm:flex-1 ${pickingMode === 0 ? 'border-blue-900 bg-blue-50' : 'border-slate-300'}`}><input className="mr-2" type="radio" checked={pickingMode === 0} onChange={() => { setPickingMode(0); setEmployeeIds((current) => current.slice(0, 1)) }} />{t('orders.picking.single')}<span className="mt-1 block pl-6 text-sm text-slate-600">{t('orders.new.singleDescription')}</span></label><label className={`cursor-pointer rounded-lg border p-4 sm:flex-1 ${pickingMode === 1 ? 'border-blue-900 bg-blue-50' : 'border-slate-300'}`}><input className="mr-2" type="radio" checked={pickingMode === 1} onChange={() => setPickingMode(1)} />{t('orders.picking.multiple')}<span className="mt-1 block pl-6 text-sm text-slate-600">{t('orders.new.multipleDescription')}</span></label></div><div className="max-h-64 overflow-y-auto rounded-lg border border-slate-300">{workforce.isLoading ? <p className="p-4 text-slate-600">{t('orders.new.workforceLoading')}</p> : workforce.isError ? <p className="p-4 text-red-700">{t('orders.new.workforceError')}</p> : workforce.data?.length === 0 ? <p className="p-4 text-slate-600">{t('orders.new.workforceEmpty')}</p> : workforce.data?.map((employee) => <label key={employee.employeeId} className="flex cursor-pointer items-center gap-3 border-b border-slate-200 p-3 last:border-0 hover:bg-slate-50"><input type={pickingMode === 0 ? 'radio' : 'checkbox'} name={pickingMode === 0 ? 'assignee' : undefined} checked={employeeIds.includes(employee.employeeId)} onChange={() => selectEmployee(employee.employeeId)} /><span><strong className="block">{employee.employeeDisplayName}</strong><small className="text-slate-600">{employee.organizationName}</small></span></label>)}</div></section>
      <section className="relative overflow-visible rounded-xl border border-slate-300 bg-white lg:col-span-12"><div className="rounded-t-xl border-b border-slate-300 bg-slate-50 p-5"><h3 className="text-lg font-semibold text-blue-950">{t('orders.itemsTitle')}</h3><div className="relative z-20 mt-4 max-w-xl"><AppIcon className="absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-500" name="search" /><input className="h-12 w-full rounded-lg border border-slate-300 bg-white pl-12 pr-4 outline-none focus:border-blue-900" value={productSearch} onChange={(e) => setProductSearch(e.target.value)} placeholder={t('orders.new.searchProduct')} />{productSearch.trim().length >= 2 && <div className="absolute inset-x-0 top-full z-50 mt-1 max-h-72 overflow-y-auto overscroll-contain rounded-lg border border-slate-300 bg-white shadow-xl">{products.isLoading ? <p className="p-4 text-sm text-slate-600">{t('orders.new.searching')}</p> : availableProducts.length === 0 ? <p className="p-4 text-sm text-slate-600">{t('orders.new.noProducts')}</p> : availableProducts.map((product) => <button type="button" key={product.id} onClick={() => addProduct(product)} className="flex w-full items-center justify-between border-b border-slate-100 p-4 text-left last:border-b-0 hover:bg-slate-50"><span><strong className="block">{product.name || t('orders.new.unnamedProduct')}</strong><small className="text-slate-600">{product.symbol || `ID: ${product.id}`}</small></span><AppIcon name="add" /></button>)}</div>}</div></div>
        {items.length === 0 ? <div className="grid min-h-48 place-items-center p-6 text-center text-slate-600"><div><AppIcon className="mx-auto mb-2 size-8" name="box" /><p>{t('orders.new.addFirstItem')}</p></div></div> : <div className="overflow-x-auto"><table className="w-full min-w-[680px] text-left"><thead className="bg-slate-100 text-sm text-slate-600"><tr><th className="p-4">{t('orders.product')}</th><th className="p-4">{t('orders.unit')}</th><th className="p-4">{t('orders.quantity')}</th><th className="p-4">{t('orders.unitWeight')}</th><th className="p-4"></th></tr></thead><tbody className="divide-y divide-slate-200">{items.map((item) => <tr key={item.product.id}><td className="p-4"><strong className="block">{item.product.name}</strong><small className="text-slate-500">{item.product.symbol}</small></td><td className="p-4">{item.product.unit}</td><td className="p-4"><input aria-label={`${t('orders.quantity')} ${item.product.name}`} className="h-10 w-28 rounded border border-slate-300 px-3 text-right" type="number" min="0.0001" step="0.0001" value={item.quantity} onChange={(e) => setQuantity(Number(item.product.id), Number(e.target.value))} /></td><td className="p-4">{item.product.unitWeightKg == null ? <span className="rounded bg-amber-100 px-2 py-1 text-xs font-semibold text-amber-800">{t('orders.missing')}</span> : <span className="font-semibold">{formatWeight(Number(item.product.unitWeightKg))}</span>}</td><td className="p-4 text-right"><button aria-label={`${t('orders.new.remove')} ${item.product.name}`} className="admin-action-button text-red-700" onClick={() => setItems((current) => current.filter((x) => x.product.id !== item.product.id))}>{t('orders.new.remove')}</button></td></tr>)}</tbody></table></div>}
      </section>
    </div>
    <div className="sticky bottom-[72px] -mx-4 mt-6 flex flex-col gap-3 border-t border-slate-300 bg-white/95 p-4 shadow-[0_-4px_12px_rgba(0,0,0,.06)] backdrop-blur sm:flex-row sm:justify-end lg:bottom-0 lg:mx-0 lg:rounded-xl lg:border"><Link className="admin-action-button" to="/warehouse-orders">{t('orders.new.cancel')}</Link><button disabled={save.isPending} onClick={() => { setError(null); save.mutate(false) }} className="min-h-12 rounded-lg border-2 border-blue-950 px-5 font-semibold text-blue-950 disabled:opacity-50">{save.isPending ? t('orders.new.saving') : t('orders.new.saveDraft')}</button><button disabled={save.isPending} onClick={() => { setError(null); save.mutate(true) }} className="min-h-12 rounded-lg bg-blue-950 px-5 font-semibold text-white disabled:opacity-50">{save.isPending ? t('orders.new.saving') : t('orders.details.publish')}</button></div>
  </section>
}

function Summary({ label, value }: { label: string; value: string }) { return <div className="flex justify-between rounded-lg bg-white p-3"><dt>{label}</dt><dd className="font-bold">{value}</dd></div> }
