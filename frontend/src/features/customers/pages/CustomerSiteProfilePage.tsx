import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { customersErrorKey } from '../customersError'
import { customerKeys } from '../queryKeys'
import {
  configureCustomerSiteLogisticsProfile,
  createCustomerSite,
  getCustomer,
  getCustomerSite,
  setCustomerSiteActive,
  updateCustomerSite,
} from '../api/customersApi'

const optional = (value: FormDataEntryValue | null) => String(value ?? '').trim() || null

export function CustomerSiteProfilePage() {
  const { customerId = '', siteId } = useParams()
  const editing = Boolean(siteId)
  const { t } = useI18n()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const customerQuery = useQuery({ queryKey: customerKeys.detail(customerId), queryFn: () => getCustomer(customerId), enabled: !!customerId })
  const siteQuery = useQuery({ queryKey: customerKeys.site(customerId, siteId ?? ''), queryFn: () => getCustomerSite(customerId, siteId ?? ''), enabled: editing && !!customerId })
  const mutation = useMutation({
    mutationFn: async (form: HTMLFormElement) => {
      const data = new FormData(form)
      const isActive = Boolean(data.get('isActive'))
      const siteBody = { code: String(data.get('code') ?? '').trim(), name: String(data.get('name') ?? '').trim(), countryCode: String(data.get('countryCode') ?? '').trim().toUpperCase() }
      let site = editing && siteId
        ? await updateCustomerSite(customerId, siteId, { ...siteBody, version: siteQuery.data!.version })
        : await createCustomerSite(customerId, { ...siteBody, isActive, customerVersion: customerQuery.data!.version })
      if (site.isActive !== isActive) site = await setCustomerSiteActive(customerId, site.id, isActive, site.version)
      const heightValue = optional(data.get('maximumPalletHeightCm'))
      return configureCustomerSiteLogisticsProfile(customerId, site.id, {
        recipientName: optional(data.get('recipientName')), street: optional(data.get('street')), postalCode: optional(data.get('postalCode')),
        city: optional(data.get('city')), defaultDock: optional(data.get('defaultDock')), receivingHours: optional(data.get('receivingHours')),
        supplierNumber: optional(data.get('supplierNumber')), defaultPalletType: optional(data.get('defaultPalletType')),
        maximumPalletHeightCm: heightValue === null ? null : Number(heightValue), requiresStretchFilm: Boolean(data.get('requiresStretchFilm')),
        requiresStraps: Boolean(data.get('requiresStraps')), requiresCornerProtectors: Boolean(data.get('requiresCornerProtectors')),
        loadSecuringNotes: optional(data.get('loadSecuringNotes')), labelProfile: data.get('labelProfile') === 'vda4902' ? 0 : null,
        version: site.version,
      })
    },
    onSuccess: async (site) => { await queryClient.invalidateQueries({ queryKey: customerKeys.all }); navigate(`/customers/${site.customerId}`) },
  })
  if (customerQuery.isLoading || (editing && siteQuery.isLoading)) return <p role="status">{t('customers.loading')}</p>
  if (customerQuery.isError || (editing && siteQuery.isError) || !customerQuery.data || (editing && !siteQuery.data)) return <p role="alert">{t(customerQuery.isError ? customersErrorKey(customerQuery.error) : siteQuery.isError ? customersErrorKey(siteQuery.error) : 'customers.error.notFound')}</p>
  const site = siteQuery.data
  const profile = site?.logisticsProfile
  function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); mutation.mutate(event.currentTarget) }
  return <section className="mx-auto max-w-3xl"><Link className="mb-4 inline-flex min-h-11 items-center gap-2 font-semibold text-blue-950 hover:underline" to={`/customers/${customerId}`}><AppIcon name="arrowBack" />{t('customers.back')}</Link><h2 className="text-2xl font-bold">{t(editing ? 'customers.editSite' : 'customers.createSite')}</h2><form className="mt-6 space-y-6" key={site?.id ?? 'new'} onSubmit={submit}><fieldset className="rounded-xl border border-slate-300 bg-white p-5"><legend className="px-1 text-lg font-semibold">{t('customers.siteData')}</legend><div className="grid gap-4 sm:grid-cols-2"><Field defaultValue={site?.code} label={t('customers.code')} name="code" required /><Field defaultValue={site?.name} label={t('customers.siteName')} name="name" required /><Field defaultValue={site?.countryCode} label={t('customers.country')} maxLength={2} name="countryCode" required /><label className="flex min-h-12 items-center gap-3 pt-6"><input defaultChecked={site?.isActive ?? true} name="isActive" type="checkbox" /><span className="font-semibold">{t('customers.active')}</span></label></div></fieldset><fieldset className="rounded-xl border border-slate-300 bg-white p-5"><legend className="px-1 text-lg font-semibold">{t('customers.address')}</legend><div className="grid gap-4 sm:grid-cols-2"><Field defaultValue={profile?.recipientName} label={t('customers.recipientName')} name="recipientName" /><Field defaultValue={profile?.street} label={t('customers.street')} name="street" /><Field defaultValue={profile?.postalCode} label={t('customers.postalCode')} name="postalCode" /><Field defaultValue={profile?.city} label={t('customers.city')} name="city" /></div></fieldset><fieldset className="rounded-xl border border-slate-300 bg-white p-5"><legend className="px-1 text-lg font-semibold">{t('customers.delivery')}</legend><div className="grid gap-4 sm:grid-cols-2"><Field defaultValue={profile?.defaultDock} label={t('customers.dock')} name="defaultDock" /><Field defaultValue={profile?.receivingHours} label={t('customers.receivingHours')} name="receivingHours" /><Field defaultValue={profile?.supplierNumber} label={t('customers.supplierNumber')} name="supplierNumber" /></div></fieldset><fieldset className="rounded-xl border border-slate-300 bg-white p-5"><legend className="px-1 text-lg font-semibold">{t('customers.packaging')}</legend><div className="grid gap-4 sm:grid-cols-2"><Field defaultValue={profile?.defaultPalletType} label={t('customers.defaultPallet')} name="defaultPalletType" /><Field defaultValue={profile?.maximumPalletHeightCm?.toString()} label={t('customers.maximumHeight')} name="maximumPalletHeightCm" type="number" /><label className="flex items-center gap-3"><input defaultChecked={profile?.requiresStretchFilm ?? false} name="requiresStretchFilm" type="checkbox" />{t('customers.stretchFilm')}</label><label className="flex items-center gap-3"><input defaultChecked={profile?.requiresStraps ?? false} name="requiresStraps" type="checkbox" />{t('customers.straps')}</label><label className="flex items-center gap-3"><input defaultChecked={profile?.requiresCornerProtectors ?? false} name="requiresCornerProtectors" type="checkbox" />{t('customers.cornerProtectors')}</label></div><label className="mt-4 block"><span className="font-semibold">{t('customers.requirementNotes')}</span><textarea className="mt-2 min-h-20 w-full rounded-lg border border-slate-300 p-3" defaultValue={profile?.loadSecuringNotes ?? ''} name="loadSecuringNotes" /></label></fieldset><fieldset className="rounded-xl border border-slate-300 bg-white p-5"><legend className="px-1 text-lg font-semibold">{t('customers.vda')}</legend><label className="flex min-h-12 items-center gap-3"><input defaultChecked={profile?.labelProfile === 0} name="labelProfile" type="checkbox" value="vda4902" />{t('customers.vda4902')}</label>{profile && <p className={`mt-3 text-sm font-semibold ${profile.isComplete ? 'text-emerald-800' : 'text-amber-900'}`}>{profile.isComplete ? t('customers.profileComplete') : t('customers.profileDraft')}</p>}</fieldset>{mutation.isError && <p className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">{t(customersErrorKey(mutation.error))}</p>}<div className="flex justify-end gap-3"><Link className="admin-action-button" to={`/customers/${customerId}`}>{t('customers.cancel')}</Link><button className="min-h-12 rounded-lg border border-slate-300 bg-white px-5 font-semibold" disabled={mutation.isPending} type="submit">{t('customers.saveDraft')}</button><button className="min-h-12 rounded-lg bg-blue-950 px-5 font-semibold text-white disabled:opacity-60" disabled={mutation.isPending} type="submit">{mutation.isPending ? t('customers.saving') : t('customers.save')}</button></div></form></section>
}

function Field({ label, name, defaultValue, required, maxLength = 120, type = 'text' }: { label: string; name: string; defaultValue?: string | null; required?: boolean; maxLength?: number; type?: string }) { return <label className="block"><span className="font-semibold">{label}{required && <span className="text-red-700"> *</span>}</span><input className="mt-2 h-12 w-full rounded-lg border border-slate-300 px-4" defaultValue={defaultValue ?? ''} maxLength={maxLength} name={name} required={required} type={type} /></label> }
