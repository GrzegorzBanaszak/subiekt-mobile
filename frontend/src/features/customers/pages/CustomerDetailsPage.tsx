import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { customersErrorKey } from '../customersError'
import { customerKeys } from '../queryKeys'
import { getCustomer, getCustomerActivity, getCustomerSites, setCustomerActive } from '../api/customersApi'

type Tab = 'data' | 'sites' | 'activity'

export function CustomerDetailsPage() {
  const { customerId = '' } = useParams()
  const { language, t } = useI18n()
  const queryClient = useQueryClient()
  const [tab, setTab] = useState<Tab>('data')
  const [actionError, setActionError] = useState<string | null>(null)
  const customerQuery = useQuery({ queryKey: customerKeys.detail(customerId), queryFn: () => getCustomer(customerId), enabled: !!customerId })
  const sitesQuery = useQuery({ queryKey: customerKeys.sites(customerId, '', 1), queryFn: () => getCustomerSites(customerId, '', 1, 50), enabled: !!customerId && tab === 'sites' })
  const activityQuery = useQuery({ queryKey: customerKeys.activity(customerId), queryFn: () => getCustomerActivity(customerId, 1, 50), enabled: !!customerId && tab === 'activity' })
  const activeMutation = useMutation({
    mutationFn: () => setCustomerActive(customerId, !customerQuery.data!.isActive, customerQuery.data!.version),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: customerKeys.all }),
    onError: (error) => { setActionError(t(customersErrorKey(error))); void customerQuery.refetch() },
  })
  if (customerQuery.isLoading) return <p role="status">{t('customers.loading')}</p>
  if (customerQuery.isError || !customerQuery.data) return <p role="alert">{t(customerQuery.isError ? customersErrorKey(customerQuery.error) : 'customers.error.notFound')}</p>
  const customer = customerQuery.data
  const date = new Intl.DateTimeFormat(language === 'pl' ? 'pl-PL' : 'es-ES', { dateStyle: 'medium', timeStyle: 'short' })
  function toggleActive() {
    const next = !customer.isActive
    const message = t(next ? 'customers.confirmActivate' : 'customers.confirmDeactivate').replace('{name}', customer.name)
    if (window.confirm(message)) activeMutation.mutate()
  }
  return <section className="mx-auto max-w-[1400px]"><Link className="mb-4 inline-flex min-h-11 items-center gap-2 font-semibold text-blue-950 hover:underline" to="/customers"><AppIcon name="arrowBack" />{t('customers.back')}</Link><div className="flex flex-col gap-4 rounded-xl border border-slate-300 bg-white p-5 sm:flex-row sm:items-start sm:justify-between"><div><div className="flex flex-wrap items-center gap-3"><h2 className="text-2xl font-bold">{customer.name}</h2><Status active={customer.isActive} /></div><p className="mt-1 font-mono text-sm text-slate-600">{customer.code}</p><p className="mt-2 text-sm text-slate-600">{t('customers.updated').replace('{date}', date.format(new Date(customer.updatedAtUtc)))}</p></div><div className="flex flex-wrap gap-2"><Link className="admin-action-button" to={`/customers/${customer.id}/edit`}><AppIcon className="size-5" name="edit" />{t('customers.edit')}</Link><button className="admin-action-button" disabled={activeMutation.isPending} onClick={toggleActive} type="button">{customer.isActive ? t('customers.deactivate') : t('customers.activate')}</button></div></div>{actionError && <p className="mt-4 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">{actionError}</p>}<nav className="mt-6 flex gap-1 overflow-x-auto border-b border-slate-300" aria-label={t('customers.title')}>{([['data', 'customers.customerData'], ['sites', 'customers.sites'], ['activity', 'customers.activity']] as const).map(([value, key]) => <button className={`min-h-12 whitespace-nowrap border-b-2 px-4 font-semibold ${tab === value ? 'border-blue-950 text-blue-950' : 'border-transparent text-slate-600 hover:text-slate-950'}`} key={value} onClick={() => setTab(value)} type="button">{t(key)}</button>)}</nav>{tab === 'data' && <CustomerData customer={customer} />}{tab === 'sites' && <Sites customerId={customerId} query={sitesQuery} />}{tab === 'activity' && <Activity query={activityQuery} date={date} />}</section>
}

function CustomerData({ customer }: { customer: Awaited<ReturnType<typeof getCustomer>> }) {
  const { t } = useI18n()
  const contractor = customer.subiektContractorId
    ? t('customers.contractorId').replace('{id}', String(customer.subiektContractorId))
    : t('customers.unlinked')

  return <div className="mt-6 grid gap-5 rounded-xl border border-slate-300 bg-white p-5 sm:grid-cols-2">
    <Data label={t('customers.code')} value={customer.code} />
    <Data label={t('customers.taxId')} value={customer.taxId ?? '—'} />
    <Data label={t('customers.linkedContractor')} value={contractor} />
    <Data label={t('customers.status')} value={customer.isActive ? t('customers.active') : t('customers.inactive')} />
    <div className="sm:col-span-2"><Data label={t('customers.notes')} value={customer.internalNotes ?? '—'} /></div>
    <section className="sm:col-span-2 rounded-lg bg-slate-50 p-4">
      <h3 className="font-semibold">{t('customers.orderHistory')}</h3>
      <p className="mt-2 text-sm text-slate-600">{t('customers.orderHistoryEmpty')}</p>
    </section>
  </div>
}

function Sites({ customerId, query }: { customerId: string; query: ReturnType<typeof useQuery> }) { const { t } = useI18n(); if (query.isLoading) return <p className="mt-6" role="status">{t('customers.loading')}</p>; if (query.isError) return <p className="mt-6" role="alert">{t(customersErrorKey(query.error))}</p>; const sites = (query.data as Awaited<ReturnType<typeof getCustomerSites>> | undefined)?.items ?? []; return <div className="mt-6"><div className="mb-4 flex justify-end"><Link className="inline-flex min-h-12 items-center gap-2 rounded-lg bg-blue-950 px-4 font-semibold text-white" to={`/customers/${customerId}/sites/new`}><AppIcon name="add" />{t('customers.addSite')}</Link></div>{sites.length === 0 ? <p className="rounded-xl border border-slate-300 bg-white p-5 text-slate-600">{t('customers.siteEmpty')}</p> : <div className="grid gap-3 lg:grid-cols-2">{sites.map((site) => <Link className="rounded-xl border border-slate-300 bg-white p-5 hover:border-blue-900" key={site.id} to={`/customers/${customerId}/sites/${site.id}`}><div className="flex items-start justify-between gap-4"><div><p className="font-semibold">{site.name}</p><p className="mt-1 font-mono text-sm text-slate-600">{site.code} · {site.countryCode}</p></div><span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${site.hasCompleteProfile ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-900'}`}>{site.hasCompleteProfile ? t('customers.profileComplete') : t('customers.profileDraft')}</span></div><p className="mt-4 text-sm text-slate-600">{site.defaultDock ?? '—'} · {site.supplierNumber ?? '—'}</p></Link>)}</div>}</div> }

function Activity({ query, date }: { query: ReturnType<typeof useQuery>; date: Intl.DateTimeFormat }) { const { t } = useI18n(); if (query.isLoading) return <p className="mt-6" role="status">{t('customers.loading')}</p>; if (query.isError) return <p className="mt-6" role="alert">{t(customersErrorKey(query.error))}</p>; const items = (query.data as Awaited<ReturnType<typeof getCustomerActivity>> | undefined)?.items ?? []; return <div className="mt-6 rounded-xl border border-slate-300 bg-white">{items.length === 0 ? <p className="p-5 text-slate-600">{t('customers.activityEmpty')}</p> : <ul className="divide-y divide-slate-200">{items.map((item) => <li className="p-4" key={item.id}><p className="font-semibold">{item.action}</p><p className="mt-1 text-sm text-slate-600">{item.actorDisplayName} · {date.format(new Date(item.occurredAtUtc))}</p></li>)}</ul>}</div> }

function Data({ label, value }: { label: string; value: string }) { return <div><dt className="text-sm text-slate-500">{label}</dt><dd className="mt-1 font-semibold">{value}</dd></div> }
function Status({ active }: { active: boolean }) { const { t } = useI18n(); return <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'}`}>{active ? t('customers.active') : t('customers.inactive')}</span> }
