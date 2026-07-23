import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiRequestError } from '../../../api/apiError'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { convertCustomerOrder, getCustomerOrder } from '../api/customerOrdersApi'

export function CustomerOrderDetailsPage() {
  const { t, language } = useI18n()
  const { customerOrderId = '' } = useParams()
  const sourceDocumentId = Number(customerOrderId)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const orderQuery = useQuery({ queryKey: ['customer-order', sourceDocumentId], queryFn: () => getCustomerOrder(sourceDocumentId), enabled: Number.isInteger(sourceDocumentId) && sourceDocumentId > 0 })
  const conversion = useMutation({ mutationFn: () => convertCustomerOrder(sourceDocumentId), onSuccess: (result) => navigate(`/warehouse-orders/${result.warehouseOrderId}`), onError: () => void queryClient.invalidateQueries({ queryKey: ['customer-order', sourceDocumentId] }) })
  if (orderQuery.isLoading) return <p role="status">{t('customerOrders.loading')}</p>
  if (orderQuery.isError || !orderQuery.data) return <p role="alert">{t('customerOrders.error')}</p>
  const order = orderQuery.data
  const date = new Intl.DateTimeFormat(language === 'pl' ? 'pl-PL' : 'es-ES')
  const error = conversion.error instanceof ApiRequestError ? conversion.error : null

  return <section className="mx-auto max-w-5xl"><Link className="mb-4 inline-flex items-center gap-2 font-semibold text-blue-950 hover:underline" to="/customer-orders"><AppIcon name="arrowBack" />{t('customerOrders.back')}</Link><div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between"><div><h2 className="text-2xl font-bold">{order.number}</h2><p className="mt-2 text-slate-600">{order.customerName}</p></div>{order.warehouseOrderId ? <Link className="admin-action-button" to={`/warehouse-orders/${order.warehouseOrderId}`}>{t('customerOrders.openWarehouseOrder')}</Link> : <button className="min-h-11 rounded-lg bg-emerald-700 px-4 font-semibold text-white disabled:opacity-50" disabled={conversion.isPending} onClick={() => conversion.mutate()}>{conversion.isPending ? t('customerOrders.converting') : t('customerOrders.convert')}</button>}</div>{error && <p className="mt-5 rounded-lg border border-red-300 bg-red-50 p-4 text-red-900" role="alert">{t(error.status === 409 ? 'customerOrders.conflict' : 'customerOrders.operationError')}</p>}<div className="mt-6 grid gap-5 lg:grid-cols-12"><section className="rounded-xl border border-slate-300 bg-white p-5 lg:col-span-5"><h3 className="border-b border-slate-200 pb-3 text-lg font-semibold">{t('customerOrders.sourceData')}</h3><dl className="mt-4 grid gap-4"><Data label={t('customerOrders.issuedDate')} value={date.format(new Date(`${order.issuedDate}T00:00:00`))} /><Data label={t('customerOrders.deliveryDate')} value={date.format(new Date(`${order.requestedDeliveryDate}T00:00:00`))} /><Data label={t('customerOrders.notes')} value={order.notes ?? '—'} /></dl></section><section className="overflow-hidden rounded-xl border border-slate-300 bg-white lg:col-span-12"><div className="border-b border-slate-300 bg-slate-50 p-5"><h3 className="text-lg font-semibold">{t('customerOrders.items')}</h3></div><div className="overflow-x-auto"><table className="w-full min-w-[560px] text-left"><thead className="bg-slate-100 text-sm text-slate-600"><tr><th className="p-4">{t('customerOrders.product')}</th><th className="p-4">{t('customerOrders.symbol')}</th><th className="p-4 text-right">{t('customerOrders.quantity')}</th><th className="p-4">{t('customerOrders.unit')}</th></tr></thead><tbody className="divide-y divide-slate-200">{order.items.map((item) => <tr key={item.sourceItemId}><td className="p-4 font-semibold">{item.productName}</td><td className="p-4">{item.productSymbol ?? '—'}</td><td className="p-4 text-right">{item.quantity}</td><td className="p-4">{item.unit}</td></tr>)}</tbody></table></div></section></div></section>
}

function Data({ label, value }: { label: string; value: string }) { return <div><dt className="text-sm text-slate-500">{label}</dt><dd className="mt-1 font-semibold">{value}</dd></div> }
