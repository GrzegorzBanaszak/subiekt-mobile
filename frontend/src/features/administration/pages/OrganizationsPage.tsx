import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { administrationErrorKey } from '../administrationError'
import {
  createOrganization,
  getOrganizations,
  setOrganizationActive,
  updateOrganization,
  type Organization,
} from '../api/administrationApi'
import { AdministrationDialog } from '../components/AdministrationDialog'
import { AdministrationForm, FormField } from '../components/AdministrationForm'
import { AdministrationError, AdministrationLoading, StatusBadge } from '../components/AdministrationState'
import { administrationKeys } from '../queryKeys'

type DialogState = { type: 'create' } | { type: 'edit'; organization: Organization } | null

export function OrganizationsPage() {
  const { t } = useI18n()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [dialog, setDialog] = useState<DialogState>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const organizationsQuery = useQuery({ queryKey: administrationKeys.organizations, queryFn: getOrganizations })

  function closeDialog() {
    setDialog(null)
    setFormError(null)
  }
  function mutationError(error: unknown) {
    setFormError(t(administrationErrorKey(error)))
  }
  const createMutation = useMutation({
    mutationFn: createOrganization,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: administrationKeys.organizations })
      closeDialog()
    },
    onError: mutationError,
  })
  const updateMutation = useMutation({
    mutationFn: ({ id, code, name }: { id: string; code: string; name: string }) =>
      updateOrganization(id, { code, name }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: administrationKeys.organizations })
      closeDialog()
    },
    onError: mutationError,
  })
  const activeMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => setOrganizationActive(id, isActive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: administrationKeys.organizations }),
    onError: (error) => setActionError(t(administrationErrorKey(error))),
  })

  const organizations = useMemo(
    () => organizationsQuery.data ?? [],
    [organizationsQuery.data],
  )
  const filteredOrganizations = useMemo(() => {
    const value = search.trim().toLocaleLowerCase()
    if (!value) return organizations
    return organizations.filter((organization) =>
      `${organization.code} ${organization.name}`.toLocaleLowerCase().includes(value),
    )
  }, [organizations, search])

  function submitOrganization(form: HTMLFormElement) {
    const data = new FormData(form)
    const code = String(data.get('code') ?? '').trim()
    const name = String(data.get('name') ?? '').trim()
    setFormError(null)
    if (dialog?.type === 'edit') updateMutation.mutate({ id: dialog.organization.id, code, name })
    else createMutation.mutate({ code, name })
  }

  function toggleActive(organization: Organization) {
    setActionError(null)
    const nextActive = !organization.isActive
    const message = nextActive ? t('administration.confirm.activateOrganization') : t('administration.confirm.deactivateOrganization')
    if (window.confirm(message.replace('{name}', organization.name))) {
      activeMutation.mutate({ id: organization.id, isActive: nextActive })
    }
  }

  if (organizationsQuery.isLoading) return <AdministrationLoading label={t('administration.loading')} />
  if (organizationsQuery.isError) {
    return <AdministrationError label={t(administrationErrorKey(organizationsQuery.error))} onRetry={() => void organizationsQuery.refetch()} retryLabel={t('administration.retry')} />
  }

  const pending = createMutation.isPending || updateMutation.isPending
  return (
    <div>
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <label className="relative block w-full sm:max-w-sm">
          <span className="sr-only">{t('administration.searchOrganizations')}</span>
          <AppIcon className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-500" name="search" />
          <input aria-label={t('administration.searchOrganizations')} className="h-12 w-full rounded-lg border border-slate-300 bg-white pl-12 pr-4 outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20" onChange={(event) => setSearch(event.target.value)} placeholder={t('administration.searchOrganizations')} type="search" value={search} />
        </label>
        <button className="flex min-h-12 items-center justify-center gap-2 rounded-lg bg-blue-950 px-4 font-semibold text-white hover:bg-blue-900" onClick={() => setDialog({ type: 'create' })} type="button">
          <AppIcon name="add" />{t('administration.addOrganization')}
        </button>
      </div>
      {actionError && <p className="mb-4 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">{actionError}</p>}
      {filteredOrganizations.length === 0 ? (
        <div className="grid min-h-52 place-items-center rounded-xl border border-slate-300 bg-white text-slate-600">{t('administration.noOrganizations')}</div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-300 bg-white">
          <ul className="divide-y divide-slate-200">
            {filteredOrganizations.map((organization) => (
              <li className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between" key={organization.id}>
                <Link className="min-w-0 flex-1 rounded-lg hover:text-blue-900 focus-visible:outline-2 focus-visible:outline-blue-900" to={`/administration/organizations/${organization.id}`}>
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-semibold">{organization.name}</p>
                    <StatusBadge active={organization.isActive} activeLabel={t('administration.active')} inactiveLabel={t('administration.inactive')} />
                  </div>
                  <p className="mt-1 font-mono text-sm text-slate-600">{organization.code}</p>
                </Link>
                <div className="flex flex-wrap gap-2">
                  <button className="admin-action-button" onClick={() => setDialog({ type: 'edit', organization })} type="button"><AppIcon className="size-5" name="edit" />{t('administration.edit')}</button>
                  <button className="admin-action-button" disabled={activeMutation.isPending} onClick={() => toggleActive(organization)} type="button"><AppIcon className="size-5" name={organization.isActive ? 'block' : 'check'} />{organization.isActive ? t('administration.deactivate') : t('administration.activate')}</button>
                  <Link className="admin-action-button" to={`/administration/organizations/${organization.id}`}>{t('administration.employees')}</Link>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      {dialog && (
        <AdministrationDialog closeLabel={t('administration.close')} onClose={closeDialog} title={t(dialog.type === 'create' ? 'administration.createOrganization' : 'administration.editOrganization')}>
          <AdministrationForm cancelLabel={t('administration.cancel')} error={formError} isPending={pending} onCancel={closeDialog} onSubmit={submitOrganization} pendingLabel={t('administration.saving')} submitLabel={t('administration.save')}>
            <FormField defaultValue={dialog.type === 'edit' ? dialog.organization.code : ''} label={t('administration.code')} maxLength={32} minLength={2} name="code" pattern="[\p{L}\p{N}_-]+" />
            <FormField defaultValue={dialog.type === 'edit' ? dialog.organization.name : ''} label={t('administration.organizationName')} maxLength={120} minLength={2} name="name" />
          </AdministrationForm>
        </AdministrationDialog>
      )}
    </div>
  )
}
