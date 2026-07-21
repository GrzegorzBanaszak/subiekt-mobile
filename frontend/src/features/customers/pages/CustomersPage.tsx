import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useState } from 'react'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { customersErrorKey } from '../customersError'
import { customerKeys } from '../queryKeys'
import { getCustomers, setCustomerActive, type CustomerListItem } from '../api/customersApi'

const pageSize = 20

export function CustomersPage() {
  const { language, t } = useI18n()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [active, setActive] = useState<boolean | undefined>()
  const [page, setPage] = useState(1)
  const [actionError, setActionError] = useState<string | null>(null)
  const query = useQuery({
    queryKey: customerKeys.list(search, active, page),
    queryFn: () => getCustomers(search, active, page, pageSize),
    placeholderData: keepPreviousData,
  })
  const activeMutation = useMutation({
    mutationFn: ({ customer, isActive }: { customer: CustomerListItem; isActive: boolean }) =>
      setCustomerActive(customer.id, isActive, customer.version),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: customerKeys.all }),
    onError: (error) => setActionError(t(customersErrorKey(error))),
  })
  const totalPages = Number(query.data?.totalPages ?? 0)
  const date = new Intl.DateTimeFormat(language === 'pl' ? 'pl-PL' : 'es-ES', { dateStyle: 'medium', timeStyle: 'short' })

  function changeSearch(value: string) { setSearch(value); setPage(1) }
  function changeActive(value: string) {
    setActive(value === 'active' ? true : value === 'inactive' ? false : undefined)
    setPage(1)
  }
  function toggleActive(customer: CustomerListItem) {
    const isActive = !customer.isActive
    const message = t(isActive ? 'customers.confirmActivate' : 'customers.confirmDeactivate').replace('{name}', customer.name)
    if (window.confirm(message)) activeMutation.mutate({ customer, isActive })
  }

  return <section className="mx-auto max-w-[1400px]" aria-labelledby="customers-heading">
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div><h2 id="customers-heading" className="text-2xl font-bold tracking-tight lg:text-3xl">{t('customers.title')}</h2><p className="mt-1 text-sm text-slate-600">{query.isLoading ? t('customers.loading') : t('customers.count').replace('{count}', String(query.data?.totalCount ?? 0))}</p></div>
      <Link className="inline-flex min-h-12 items-center justify-center gap-2 rounded-lg bg-blue-950 px-5 font-semibold text-white hover:bg-blue-900" to="/customers/new"><AppIcon className="size-5" name="add" />{t('customers.new')}</Link>
    </div>
    <p className="mb-5 text-sm text-slate-600">{t('customers.description')}</p>
    <div className="mb-5 flex flex-col gap-3 sm:flex-row">
      <label className="relative block w-full sm:max-w-md"><span className="sr-only">{t('customers.search')}</span><AppIcon className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-500" name="search" /><input className="h-12 w-full rounded-lg border border-slate-300 bg-white pl-12 pr-4 outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20" onChange={(event) => changeSearch(event.target.value)} placeholder={t('customers.searchPlaceholder')} type="search" value={search} /></label>
      <select aria-label={t('customers.status')} className="h-12 rounded-lg border border-slate-300 bg-white px-4" onChange={(event) => changeActive(event.target.value)} value={active === true ? 'active' : active === false ? 'inactive' : ''}><option value="">{t('customers.all')}</option><option value="active">{t('customers.active')}</option><option value="inactive">{t('customers.inactive')}</option></select>
    </div>
    {actionError && <p className="mb-4 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">{actionError}</p>}
    {query.isLoading ? <State text={t('customers.loading')} /> : query.isError ? <State error onRetry={() => void query.refetch()} text={t(customersErrorKey(query.error))} /> : (query.data?.items.length ?? 0) === 0 ? <State text={t('customers.empty')} /> : <><div className="hidden overflow-hidden rounded-xl border border-slate-300 bg-white md:block"><table className="w-full text-left"><thead className="border-b border-slate-300 bg-slate-100 text-sm text-slate-600"><tr><th className="p-4">{t('customers.code')}</th><th className="p-4">{t('customers.name')}</th><th className="p-4">{t('customers.siteCount')}</th><th className="p-4">{t('customers.completeProfiles')}</th><th className="p-4">{t('customers.status')}</th><th className="p-4">{t('customers.updated').replace(': {date}', '')}</th><th className="p-4"><span className="sr-only">{t('customers.actions')}</span></th></tr></thead><tbody className="divide-y divide-slate-200">{query.data!.items.map((customer) => <tr className="hover:bg-slate-50" key={customer.id}><td className="p-4 font-mono text-sm">{customer.code}</td><td className="p-4 font-semibold">{customer.name}<span className="ml-2 text-sm font-normal text-slate-500">{customer.taxId}</span></td><td className="p-4">{customer.siteCount}</td><td className="p-4">{customer.completeProfileCount}/{customer.siteCount}</td><td className="p-4"><Status active={customer.isActive} /></td><td className="p-4 text-sm text-slate-600">{date.format(new Date(customer.updatedAtUtc))}</td><td className="p-4 text-right"><div className="flex justify-end gap-2"><Link className="admin-action-button" to={`/customers/${customer.id}`}>{t('customers.open')}</Link><button className="admin-action-button" disabled={activeMutation.isPending} onClick={() => toggleActive(customer)} type="button">{customer.isActive ? t('customers.deactivate') : t('customers.activate')}</button></div></td></tr>)}</tbody></table></div><div className="grid gap-3 md:hidden">{query.data!.items.map((customer) => <Link className="rounded-xl border border-slate-300 bg-white p-4 shadow-sm" key={customer.id} to={`/customers/${customer.id}`}><div className="flex items-start justify-between gap-3"><div><p className="font-semibold">{customer.name}</p><p className="font-mono text-sm text-slate-600">{customer.code}</p></div><Status active={customer.isActive} /></div><p className="mt-3 text-sm text-slate-600">{t('customers.sites')}: {customer.siteCount} · {t('customers.completeProfiles')}: {customer.completeProfileCount}/{customer.siteCount}</p></Link>)}</div></>}
    {totalPages > 1 && <nav aria-label={t('customers.pagination')} className="mt-5 flex items-center justify-between"><span className="text-sm text-slate-600">{t('customers.page').replace('{page}', String(page)).replace('{pages}', String(totalPages))}</span><div className="flex gap-2"><button className="admin-action-button" disabled={page <= 1 || query.isFetching} onClick={() => setPage((value) => value - 1)} type="button"><AppIcon name="chevronLeft" />{t('customers.previous')}</button><button className="admin-action-button" disabled={page >= totalPages || query.isFetching} onClick={() => setPage((value) => value + 1)} type="button">{t('customers.next')}<AppIcon name="chevronRight" /></button></div></nav>}
  </section>
}

function Status({ active }: { active: boolean }) { const { t } = useI18n(); return <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'}`}>{active ? t('customers.active') : t('customers.inactive')}</span> }
function State({ text, error, onRetry }: { text: string; error?: boolean; onRetry?: () => void }) { const { t } = useI18n(); return <div className={`grid min-h-52 place-items-center rounded-xl border p-6 text-center ${error ? 'border-red-300 bg-red-50 text-red-900' : 'border-slate-300 bg-white text-slate-600'}`}><div><p role={error ? 'alert' : undefined}>{text}</p>{onRetry && <button className="mt-4 min-h-11 rounded-lg bg-blue-950 px-5 font-semibold text-white" onClick={onRetry} type="button">{t('customers.retry')}</button>}</div></div> }
