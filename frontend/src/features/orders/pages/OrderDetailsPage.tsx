import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { AppIcon } from '../../../shared/components/AppIcon'
import { configureOrderPicking, deleteOrder, getAvailableOrderAssignees, getOrder, publishOrder, type Order } from '../api/ordersApi'

export function OrderDetailsPage() {
  const { orderId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['order', orderId], queryFn: () => getOrder(orderId), enabled: Boolean(orderId) })
  const publish = useMutation({
    mutationFn: (version: number) => publishOrder(orderId, version),
    onSuccess: (updated) => {
      queryClient.setQueryData(['order', orderId], updated)
      void queryClient.invalidateQueries({ queryKey: ['orders'] })
    },
  })
  const remove = useMutation({
    mutationFn: (version: number) => deleteOrder(orderId, version),
    onSuccess: async () => {
      queryClient.removeQueries({ queryKey: ['order', orderId] })
      await queryClient.invalidateQueries({ queryKey: ['orders'] })
      navigate('/orders')
    },
  })
  if (query.isLoading) return <div role="status" className="grid min-h-64 place-items-center">Ładowanie zamówienia…</div>
  if (query.isError || !query.data) return <div role="alert" className="mx-auto max-w-3xl rounded-xl border border-red-300 bg-red-50 p-8 text-center text-red-900"><AppIcon className="mx-auto mb-3 size-8" name="warning" /><h2 className="font-bold">Nie udało się pobrać zamówienia</h2><Link className="mt-4 inline-block font-semibold underline" to="/orders">Wróć do listy</Link></div>
  const order = query.data
  const published = order.status === 1 || (order.status as number | string) === 'ReadyForPicking'
  const missingWeight = order.items.filter((item) => item.unitWeightKg == null).length
  const totalWeight = order.items.reduce((sum, item) => sum + Number(item.unitWeightKg ?? 0) * Number(item.quantity), 0)

  return <section className="mx-auto max-w-[1200px]">
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between"><div className="flex items-start gap-3"><Link to="/orders" className="flex size-11 items-center justify-center rounded-full hover:bg-slate-200"><AppIcon name="arrowBack" /></Link><div><p className="text-sm font-semibold text-slate-500">Zamówienie</p><h2 className="text-2xl font-bold lg:text-3xl">{order.number}</h2><p className="mt-1 text-sm text-slate-600">Ostatnia zmiana: {formatDateTime(order.updatedAtUtc)}</p></div></div><div className="flex flex-wrap items-center gap-2"><span className={`w-fit rounded-full px-3 py-1.5 text-sm font-semibold ${published ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'}`}>{published ? 'Gotowe do kompletacji' : 'Wersja robocza'}</span>{!published && <><button className="min-h-11 rounded-lg border border-red-300 bg-white px-4 font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50" disabled={remove.isPending || publish.isPending} onClick={() => { if (window.confirm(`Usunąć zamówienie ${order.number}? Tej operacji nie można cofnąć.`)) remove.mutate(Number(order.version)) }}>Usuń</button><button className="min-h-11 rounded-lg bg-blue-950 px-4 font-semibold text-white hover:bg-blue-900 disabled:opacity-50" disabled={remove.isPending || publish.isPending} onClick={() => publish.mutate(Number(order.version))}>{publish.isPending ? 'Udostępnianie…' : 'Udostępnij do kompletacji'}</button></>}</div></div>
    {(publish.error || remove.error) && <div role="alert" className="mb-5 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900">{(publish.error ?? remove.error)?.message || 'Operacja nie powiodła się.'}</div>}
    <div className="grid gap-5 lg:grid-cols-12">
      <section className="rounded-xl border border-slate-300 bg-white p-5 lg:col-span-8"><h3 className="border-b border-slate-200 pb-3 text-lg font-semibold text-blue-950">Dane zamówienia</h3><dl className="mt-5 grid gap-5 sm:grid-cols-2"><div><dt className="text-sm text-slate-500">Zamawiający</dt><dd className="mt-1 font-semibold">{order.customerName}</dd></div><div><dt className="text-sm text-slate-500">Termin realizacji</dt><dd className="mt-1 font-semibold">{formatDate(order.dueDate)}</dd></div><div><dt className="text-sm text-slate-500">Autor</dt><dd className="mt-1 font-semibold">{order.createdByName}</dd></div><div><dt className="text-sm text-slate-500">Utworzono</dt><dd className="mt-1 font-semibold">{formatDateTime(order.createdAtUtc)}</dd></div></dl></section>
      <aside className="rounded-xl border border-slate-300 bg-slate-100 p-5 lg:col-span-4"><h3 className="text-lg font-semibold">Podsumowanie</h3><dl className="mt-4 grid gap-3"><Summary label="Pozycje" value={String(order.items.length)} /><Summary label="Łączna ilość" value={formatNumber(order.items.reduce((sum, x) => sum + Number(x.quantity), 0))} /><Summary label="Szacowana masa" value={missingWeight ? 'Niepełne dane' : `${formatNumber(totalWeight)} kg`} /></dl>{missingWeight > 0 && <div className="mt-4 flex gap-2 rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900"><AppIcon className="size-5" name="warning" /><span>Brak masy dla {missingWeight} {missingWeight === 1 ? 'pozycji' : 'pozycji'}.</span></div>}</aside>
      <PickingConfiguration key={String(order.version)} order={order} editable={!published} />
      <section className="overflow-hidden rounded-xl border border-slate-300 bg-white lg:col-span-12"><div className="border-b border-slate-300 bg-slate-50 p-5"><h3 className="text-lg font-semibold text-blue-950">Pozycje zamówienia</h3></div><div className="overflow-x-auto"><table className="w-full min-w-[680px] text-left"><thead className="bg-slate-100 text-sm text-slate-600"><tr><th className="p-4">Produkt</th><th className="p-4">Status</th><th className="p-4 text-right">Ilość</th><th className="p-4">J.m.</th><th className="p-4 text-right">Masa j.m.</th></tr></thead><tbody className="divide-y divide-slate-200">{order.items.map((item) => <tr key={item.id}><td className="p-4"><strong className="block">{item.productName}</strong><small className="text-slate-500">{item.productSymbol || `ID: ${item.productId}`}</small></td><td className="p-4"><span className="rounded-full bg-slate-100 px-2 py-1 text-xs font-semibold">Do kompletacji</span></td><td className="p-4 text-right font-semibold">{formatNumber(Number(item.quantity))}</td><td className="p-4">{item.unit}</td><td className="p-4 text-right">{item.unitWeightKg == null ? <span className="text-amber-700">Brak</span> : `${formatNumber(Number(item.unitWeightKg))} kg`}</td></tr>)}</tbody></table></div></section>
    </div>
  </section>
}

function PickingConfiguration({ order, editable }: { order: Order; editable: boolean }) {
  const queryClient = useQueryClient()
  const [mode, setMode] = useState(isSharedMode(order.pickingMode) ? 1 : 0)
  const [employeeIds, setEmployeeIds] = useState(order.assignees.map((x) => x.employeeId))
  const workforce = useQuery({ queryKey: ['order-assignees'], queryFn: getAvailableOrderAssignees, enabled: editable })
  const save = useMutation({
    mutationFn: () => configureOrderPicking(order.id, mode, employeeIds, Number(order.version)),
    onSuccess: (updated) => {
      queryClient.setQueryData(['order', order.id], updated)
      void queryClient.invalidateQueries({ queryKey: ['orders'] })
    },
  })
  const shared = mode === 1

  function select(employeeId: string) {
    setEmployeeIds((current) => shared
      ? current.includes(employeeId) ? current.filter((id) => id !== employeeId) : [...current, employeeId]
      : [employeeId])
  }

  return <section className="rounded-xl border border-slate-300 bg-white p-5 lg:col-span-12">
    <div className="flex flex-col gap-2 border-b border-slate-200 pb-3 sm:flex-row sm:items-center sm:justify-between"><div><h3 className="text-lg font-semibold text-blue-950">Kompletacja</h3><p className="text-sm text-slate-600">{shared ? 'Wiele przypisanych osób' : 'Jedna przypisana osoba'}</p></div>{editable && <button className="min-h-11 rounded-lg border border-blue-900 px-4 font-semibold text-blue-950 disabled:opacity-50" disabled={save.isPending} onClick={() => save.mutate()}>{save.isPending ? 'Zapisywanie…' : 'Zapisz przypisania'}</button>}</div>
    {!editable ? <div className="mt-4 flex flex-wrap gap-2">{order.assignees.map((employee) => <span key={employee.employeeId} className="rounded-full bg-blue-50 px-3 py-1.5 text-sm font-semibold text-blue-950">{employee.employeeDisplayName}</span>)}</div> : <><div className="my-4 flex gap-3"><label><input className="mr-2" type="radio" checked={!shared} onChange={() => { setMode(0); setEmployeeIds((current) => current.slice(0, 1)) }} />Jedna osoba</label><label><input className="mr-2" type="radio" checked={shared} onChange={() => setMode(1)} />Wiele osób</label></div><div className="max-h-56 overflow-y-auto rounded-lg border border-slate-300">{workforce.data?.map((employee) => <label key={employee.employeeId} className="flex cursor-pointer items-center gap-3 border-b border-slate-200 p-3 last:border-0"><input type={shared ? 'checkbox' : 'radio'} name={shared ? undefined : 'detail-assignee'} checked={employeeIds.includes(employee.employeeId)} onChange={() => select(employee.employeeId)} /><span><strong className="block">{employee.employeeDisplayName}</strong><small className="text-slate-600">{employee.organizationName}</small></span></label>)}</div>{save.error && <p className="mt-3 text-sm text-red-700">{save.error.message}</p>}</>}
  </section>
}

function Summary({ label, value }: { label: string; value: string }) { return <div className="flex justify-between rounded-lg bg-white p-3"><dt>{label}</dt><dd className="font-bold">{value}</dd></div> }
function isSharedMode(value: number | string) { return value === 1 || value === 'SharedTeam' }
function formatDate(value: string) { return new Intl.DateTimeFormat('pl-PL').format(new Date(`${value}T00:00:00`)) }
function formatDateTime(value: string) { return new Intl.DateTimeFormat('pl-PL', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) }
function formatNumber(value: number) { return new Intl.NumberFormat('pl-PL', { maximumFractionDigits: 4 }).format(value) }
