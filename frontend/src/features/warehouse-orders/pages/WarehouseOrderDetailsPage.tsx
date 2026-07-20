import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { configureWarehouseOrderPicking, deleteWarehouseOrder, getAvailableWarehouseOrderAssignees, getWarehouseOrder, publishWarehouseOrder, type WarehouseOrder } from '../api/warehouseOrdersApi'
import { formatWarehouseOrderDate, formatWarehouseOrderDateTime, formatWarehouseOrderNumber, isPublishedWarehouseOrder } from '../warehouseOrderFormat'

export function WarehouseOrderDetailsPage() {
  const { language, t } = useI18n()
  const { warehouseOrderId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['warehouse-order', warehouseOrderId], queryFn: () => getWarehouseOrder(warehouseOrderId), enabled: Boolean(warehouseOrderId) })
  const publish = useMutation({
    mutationFn: (version: number) => publishWarehouseOrder(warehouseOrderId, version),
    onSuccess: (updated) => {
      queryClient.setQueryData(['warehouse-order', warehouseOrderId], updated)
      void queryClient.invalidateQueries({ queryKey: ['warehouse-orders'] })
    },
  })
  const remove = useMutation({
    mutationFn: (version: number) => deleteWarehouseOrder(warehouseOrderId, version),
    onSuccess: async () => {
      queryClient.removeQueries({ queryKey: ['warehouse-order', warehouseOrderId] })
      await queryClient.invalidateQueries({ queryKey: ['warehouse-orders'] })
      navigate('/warehouse-orders')
    },
  })

  if (query.isLoading) return <div role="status" className="grid min-h-64 place-items-center">{t('orders.details.loading')}</div>
  if (query.isError || !query.data) return <div role="alert" className="mx-auto max-w-3xl rounded-xl border border-red-300 bg-red-50 p-8 text-center text-red-900"><AppIcon className="mx-auto mb-3 size-8" name="warning" /><h2 className="font-bold">{t('orders.details.error')}</h2><Link className="mt-4 inline-block font-semibold underline" to="/warehouse-orders">{t('orders.details.back')}</Link></div>

  const warehouseOrder = query.data
  const published = isPublishedWarehouseOrder(warehouseOrder.status)
  const missingWeight = warehouseOrder.items.filter((item) => item.unitWeightKg == null).length
  const totalWeight = warehouseOrder.items.reduce((sum, item) => sum + Number(item.unitWeightKg ?? 0) * Number(item.quantity), 0)
  const confirmDelete = () => window.confirm(t('orders.details.confirmDelete').replace('{number}', warehouseOrder.number))

  return <section className="mx-auto max-w-[1200px]">
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between"><div className="flex items-start gap-3"><Link aria-label={t('orders.details.back')} to="/warehouse-orders" className="flex size-11 items-center justify-center rounded-full hover:bg-slate-200"><AppIcon name="arrowBack" /></Link><div><p className="text-sm font-semibold text-slate-500">{t('orders.details.eyebrow')}</p><h2 className="text-2xl font-bold lg:text-3xl">{warehouseOrder.number}</h2><p className="mt-1 text-sm text-slate-600">{t('orders.details.lastChange').replace('{date}', formatWarehouseOrderDateTime(warehouseOrder.updatedAtUtc, language))}</p></div></div><div className="flex flex-wrap items-center gap-2"><span className={`w-fit rounded-full px-3 py-1.5 text-sm font-semibold ${published ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'}`}>{t(published ? 'orders.status.ready' : 'orders.status.draft')}</span>{!published && <><button className="min-h-11 rounded-lg border border-red-300 bg-white px-4 font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50" disabled={remove.isPending || publish.isPending} onClick={() => { if (confirmDelete()) remove.mutate(Number(warehouseOrder.version)) }}>{t('orders.details.delete')}</button><button className="min-h-11 rounded-lg bg-blue-950 px-4 font-semibold text-white hover:bg-blue-900 disabled:opacity-50" disabled={remove.isPending || publish.isPending} onClick={() => publish.mutate(Number(warehouseOrder.version))}>{publish.isPending ? t('orders.details.publishing') : t('orders.details.publish')}</button></>}</div></div>
    {(publish.error || remove.error) && <div role="alert" className="mb-5 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900">{t('orders.details.operationError')}</div>}
    <div className="grid gap-5 lg:grid-cols-12">
      <section className="rounded-xl border border-slate-300 bg-white p-5 lg:col-span-8"><h3 className="border-b border-slate-200 pb-3 text-lg font-semibold text-blue-950">{t('orders.details.data')}</h3><dl className="mt-5 grid gap-5 sm:grid-cols-2"><div><dt className="text-sm text-slate-500">{t('orders.details.customer')}</dt><dd className="mt-1 font-semibold">{warehouseOrder.customerName}</dd></div><div><dt className="text-sm text-slate-500">{t('orders.details.dueDate')}</dt><dd className="mt-1 font-semibold">{formatWarehouseOrderDate(warehouseOrder.dueDate, language)}</dd></div><div><dt className="text-sm text-slate-500">{t('orders.details.author')}</dt><dd className="mt-1 font-semibold">{warehouseOrder.createdByName}</dd></div><div><dt className="text-sm text-slate-500">{t('orders.details.created')}</dt><dd className="mt-1 font-semibold">{formatWarehouseOrderDateTime(warehouseOrder.createdAtUtc, language)}</dd></div></dl></section>
      <aside className="rounded-xl border border-slate-300 bg-slate-100 p-5 lg:col-span-4"><h3 className="text-lg font-semibold">{t('orders.summary')}</h3><dl className="mt-4 grid gap-3"><Summary label={t('orders.items')} value={String(warehouseOrder.items.length)} /><Summary label={t('orders.totalQuantity')} value={formatWarehouseOrderNumber(warehouseOrder.items.reduce((sum, x) => sum + Number(x.quantity), 0), language)} /><Summary label={t('orders.estimatedWeight')} value={missingWeight ? t('orders.incompleteData') : `${formatWarehouseOrderNumber(totalWeight, language)} kg`} /></dl>{missingWeight > 0 && <div className="mt-4 flex gap-2 rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900"><AppIcon className="size-5" name="warning" /><span>{t('orders.missingWeight').replace('{count}', String(missingWeight))}</span></div>}</aside>
      <WarehouseOrderPickingConfiguration key={String(warehouseOrder.version)} warehouseOrder={warehouseOrder} editable={!published} />
      <section className="overflow-hidden rounded-xl border border-slate-300 bg-white lg:col-span-12"><div className="border-b border-slate-300 bg-slate-50 p-5"><h3 className="text-lg font-semibold text-blue-950">{t('orders.itemsTitle')}</h3></div><div className="overflow-x-auto"><table className="w-full min-w-[680px] text-left"><thead className="bg-slate-100 text-sm text-slate-600"><tr><th className="p-4">{t('orders.product')}</th><th className="p-4">{t('orders.status')}</th><th className="p-4 text-right">{t('orders.quantity')}</th><th className="p-4">{t('orders.unit')}</th><th className="p-4 text-right">{t('orders.unitWeight')}</th></tr></thead><tbody className="divide-y divide-slate-200">{warehouseOrder.items.map((item) => <tr key={item.id}><td className="p-4"><strong className="block">{item.productName}</strong><small className="text-slate-500">{item.productSymbol || `ID: ${item.productId}`}</small></td><td className="p-4"><span className="rounded-full bg-slate-100 px-2 py-1 text-xs font-semibold">{t('orders.item.toPick')}</span></td><td className="p-4 text-right font-semibold">{formatWarehouseOrderNumber(Number(item.quantity), language)}</td><td className="p-4">{item.unit}</td><td className="p-4 text-right">{item.unitWeightKg == null ? <span className="text-amber-700">{t('orders.missing')}</span> : `${formatWarehouseOrderNumber(Number(item.unitWeightKg), language)} kg`}</td></tr>)}</tbody></table></div></section>
    </div>
  </section>
}

function WarehouseOrderPickingConfiguration({ warehouseOrder, editable }: { warehouseOrder: WarehouseOrder; editable: boolean }) {
  const { t } = useI18n()
  const queryClient = useQueryClient()
  const [mode, setMode] = useState(isSharedMode(warehouseOrder.pickingMode) ? 1 : 0)
  const [employeeIds, setEmployeeIds] = useState(warehouseOrder.assignees.map((x) => x.employeeId))
  const workforce = useQuery({ queryKey: ['warehouse-order-assignees'], queryFn: getAvailableWarehouseOrderAssignees, enabled: editable })
  const save = useMutation({
    mutationFn: () => configureWarehouseOrderPicking(warehouseOrder.id, mode, employeeIds, Number(warehouseOrder.version)),
    onSuccess: (updated) => {
      queryClient.setQueryData(['warehouse-order', warehouseOrder.id], updated)
      void queryClient.invalidateQueries({ queryKey: ['warehouse-orders'] })
    },
  })
  const shared = mode === 1

  function select(employeeId: string) {
    setEmployeeIds((current) => shared
      ? current.includes(employeeId) ? current.filter((id) => id !== employeeId) : [...current, employeeId]
      : [employeeId])
  }

  return <section className="rounded-xl border border-slate-300 bg-white p-5 lg:col-span-12">
    <div className="flex flex-col gap-2 border-b border-slate-200 pb-3 sm:flex-row sm:items-center sm:justify-between"><div><h3 className="text-lg font-semibold text-blue-950">{t('orders.picking.title')}</h3><p className="text-sm text-slate-600">{t(shared ? 'orders.picking.sharedAssigned' : 'orders.picking.singleAssigned')}</p></div>{editable && <button className="min-h-11 rounded-lg border border-blue-900 px-4 font-semibold text-blue-950 disabled:opacity-50" disabled={save.isPending} onClick={() => save.mutate()}>{save.isPending ? t('orders.picking.saving') : t('orders.picking.saveAssignments')}</button>}</div>
    {!editable ? <div className="mt-4 flex flex-wrap gap-2">{warehouseOrder.assignees.map((employee) => <span key={employee.employeeId} className="rounded-full bg-blue-50 px-3 py-1.5 text-sm font-semibold text-blue-950">{employee.employeeDisplayName}</span>)}</div> : <><div className="my-4 flex gap-3"><label><input className="mr-2" type="radio" checked={!shared} onChange={() => { setMode(0); setEmployeeIds((current) => current.slice(0, 1)) }} />{t('orders.picking.single')}</label><label><input className="mr-2" type="radio" checked={shared} onChange={() => setMode(1)} />{t('orders.picking.multiple')}</label></div><div className="max-h-56 overflow-y-auto rounded-lg border border-slate-300">{workforce.isLoading ? <p className="p-4 text-slate-600">{t('orders.new.workforceLoading')}</p> : workforce.isError ? <p className="p-4 text-red-700">{t('orders.new.workforceError')}</p> : workforce.data?.length === 0 ? <p className="p-4 text-slate-600">{t('orders.new.workforceEmpty')}</p> : workforce.data?.map((employee) => <label key={employee.employeeId} className="flex cursor-pointer items-center gap-3 border-b border-slate-200 p-3 last:border-0"><input type={shared ? 'checkbox' : 'radio'} name={shared ? undefined : 'detail-assignee'} checked={employeeIds.includes(employee.employeeId)} onChange={() => select(employee.employeeId)} /><span><strong className="block">{employee.employeeDisplayName}</strong><small className="text-slate-600">{employee.organizationName}</small></span></label>)}</div>{save.error && <p className="mt-3 text-sm text-red-700">{t('orders.picking.saveError')}</p>}</>}
  </section>
}

function Summary({ label, value }: { label: string; value: string }) { return <div className="flex justify-between rounded-lg bg-white p-3"><dt>{label}</dt><dd className="font-bold">{value}</dd></div> }
function isSharedMode(value: number | string) { return value === 1 || value === 'SharedTeam' }
