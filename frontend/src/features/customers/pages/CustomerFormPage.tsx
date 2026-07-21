import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { customersErrorKey } from '../customersError'
import { customerKeys } from '../queryKeys'
import {
  createCustomer,
  getCustomer,
  searchCustomerContractors,
  setCustomerActive,
  updateCustomer,
} from '../api/customersApi'

const optional = (value: FormDataEntryValue | null) => String(value ?? '').trim() || null

export function CustomerFormPage() {
  const { customerId } = useParams()
  const editing = Boolean(customerId)
  const { t } = useI18n()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [contractorSearch, setContractorSearch] = useState('')
  const [selectedContractorId, setSelectedContractorId] = useState<number | null | undefined>(undefined)
  const [error, setError] = useState<string | null>(null)
  const customerQuery = useQuery({
    queryKey: customerKeys.detail(customerId ?? ''),
    queryFn: () => getCustomer(customerId ?? ''),
    enabled: editing,
  })
  const contractorQuery = useQuery({
    queryKey: customerKeys.contractors(contractorSearch),
    queryFn: () => searchCustomerContractors(contractorSearch),
    enabled: contractorSearch.trim().length >= 2,
  })
  const mutation = useMutation({
    mutationFn: async (form: HTMLFormElement) => {
      const data = new FormData(form)
      const isActive = Boolean(data.get('isActive'))
      const body = {
        code: String(data.get('code') ?? '').trim(),
        name: String(data.get('name') ?? '').trim(),
        taxId: optional(data.get('taxId')),
        subiektContractorId: selectedContractorId === undefined
          ? customerQuery.data?.subiektContractorId ?? null
          : selectedContractorId,
        internalNotes: optional(data.get('internalNotes')),
      }

      if (!editing || !customerId) return createCustomer({ ...body, isActive })

      let customer = await updateCustomer(customerId, { ...body, version: customerQuery.data!.version })
      if (customer.isActive !== isActive) {
        customer = await setCustomerActive(customerId, isActive, customer.version)
      }
      return customer
    },
    onSuccess: async (customer) => {
      await queryClient.invalidateQueries({ queryKey: customerKeys.all })
      navigate(`/customers/${customer.id}`)
    },
    onError: (mutationError) => {
      setError(t(customersErrorKey(mutationError)))
      if (editing) void customerQuery.refetch()
    },
  })

  if (editing && customerQuery.isLoading) return <p role="status">{t('customers.loading')}</p>
  if (editing && (customerQuery.isError || !customerQuery.data)) {
    return <p role="alert">{t(customerQuery.isError ? customersErrorKey(customerQuery.error) : 'customers.error.notFound')}</p>
  }

  const customer = customerQuery.data
  const currentContractorId = selectedContractorId === undefined
    ? customer?.subiektContractorId ?? null
    : selectedContractorId

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    mutation.mutate(event.currentTarget)
  }

  return <section className="mx-auto max-w-3xl">
    <Link className="mb-4 inline-flex min-h-11 items-center gap-2 font-semibold text-blue-950 hover:underline" to={editing && customer ? `/customers/${customer.id}` : '/customers'}>
      <AppIcon name="arrowBack" />{t('customers.back')}
    </Link>
    <h2 className="text-2xl font-bold">{t(editing ? 'customers.editTitle' : 'customers.createTitle')}</h2>

    <form className="mt-6 space-y-6" key={customer?.id ?? 'new'} onSubmit={submit}>
      <fieldset className="rounded-xl border border-slate-300 bg-white p-5">
        <legend className="px-1 text-lg font-semibold">{t('customers.customerData')}</legend>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field defaultValue={customer?.code} label={t('customers.code')} name="code" required />
          <Field defaultValue={customer?.name} label={t('customers.name')} name="name" required />
          <Field defaultValue={customer?.taxId ?? ''} label={t('customers.taxId')} name="taxId" />
          <label className="flex min-h-12 items-center gap-3 pt-6">
            <input defaultChecked={customer?.isActive ?? true} name="isActive" type="checkbox" />
            <span className="font-semibold">{t('customers.active')}</span>
          </label>
        </div>
        <label className="mt-4 block">
          <span className="font-semibold">{t('customers.notes')}</span>
          <textarea className="mt-2 min-h-24 w-full rounded-lg border border-slate-300 p-3" defaultValue={customer?.internalNotes ?? ''} name="internalNotes" />
        </label>
      </fieldset>

      <fieldset className="rounded-xl border border-slate-300 bg-white p-5">
        <legend className="px-1 text-lg font-semibold">{t('customers.linkedContractor')}</legend>
        <label className="mt-2 block">
          <span className="sr-only">{t('customers.contractorSearch')}</span>
          <input className="h-12 w-full rounded-lg border border-slate-300 px-4" onChange={(event) => setContractorSearch(event.target.value)} placeholder={t('customers.contractorPlaceholder')} type="search" value={contractorSearch} />
        </label>
        <div className="mt-3 flex flex-wrap gap-2">
          <button className={`admin-action-button ${currentContractorId === null ? 'ring-2 ring-blue-900' : ''}`} onClick={() => setSelectedContractorId(null)} type="button">
            {t('customers.unlinked')}
          </button>
          {contractorQuery.data?.items.map((contractor) => <button className={`admin-action-button ${currentContractorId === Number(contractor.id) ? 'ring-2 ring-blue-900' : ''}`} key={contractor.id} onClick={() => setSelectedContractorId(Number(contractor.id))} type="button">
            {contractor.name} {'\u00B7'} {contractor.symbol}
          </button>)}
        </div>
        {customer?.subiektContractorId && selectedContractorId === undefined && <p className="mt-3 text-sm text-slate-600">
          {t('customers.contractorId').replace('{id}', String(customer.subiektContractorId))}
        </p>}
      </fieldset>

      {error && <p className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">{error}</p>}
      <div className="flex justify-end gap-3">
        <Link className="admin-action-button" to={editing && customer ? `/customers/${customer.id}` : '/customers'}>{t('customers.cancel')}</Link>
        <button className="min-h-12 rounded-lg bg-blue-950 px-5 font-semibold text-white disabled:opacity-60" disabled={mutation.isPending} type="submit">
          {mutation.isPending ? t('customers.saving') : t('customers.save')}
        </button>
      </div>
    </form>
  </section>
}

function Field({ label, name, defaultValue, required }: { label: string; name: string; defaultValue?: string | null; required?: boolean }) {
  return <label className="block">
    <span className="font-semibold">{label}{required && <span className="text-red-700"> *</span>}</span>
    <input className="mt-2 h-12 w-full rounded-lg border border-slate-300 px-4" defaultValue={defaultValue ?? ''} maxLength={name === 'code' ? 32 : 120} name={name} required={required} />
  </label>
}
