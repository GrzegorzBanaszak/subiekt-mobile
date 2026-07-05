import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { administrationErrorKey } from '../administrationError'
import {
  createEmployee,
  getEmployees,
  getOrganizations,
  setEmployeeActive,
  updateEmployee,
  type Employee,
} from '../api/administrationApi'
import { AdministrationDialog } from '../components/AdministrationDialog'
import { AdministrationForm, FormField } from '../components/AdministrationForm'
import { AdministrationError, AdministrationLoading, StatusBadge } from '../components/AdministrationState'
import { administrationKeys } from '../queryKeys'

type DialogState = { type: 'create' } | { type: 'edit'; employee: Employee } | null

export function OrganizationDetailsPage() {
  const { organizationId = '' } = useParams()
  const { t } = useI18n()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [dialog, setDialog] = useState<DialogState>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const organizationsQuery = useQuery({ queryKey: administrationKeys.organizations, queryFn: getOrganizations })
  const employeesQuery = useQuery({
    queryKey: administrationKeys.employees(organizationId),
    queryFn: () => getEmployees(organizationId),
    enabled: !!organizationId,
  })
  const organization = organizationsQuery.data?.find((item) => item.id === organizationId)

  function closeDialog() {
    setDialog(null)
    setFormError(null)
  }
  function mutationError(error: unknown) {
    setFormError(t(administrationErrorKey(error)))
  }
  const createMutation = useMutation({
    mutationFn: (body: { code: string; displayName: string }) => createEmployee(organizationId, body),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: administrationKeys.employees(organizationId) })
      closeDialog()
    },
    onError: mutationError,
  })
  const updateMutation = useMutation({
    mutationFn: ({ id, code, displayName }: { id: string; code: string; displayName: string }) =>
      updateEmployee(organizationId, id, { code, displayName }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: administrationKeys.employees(organizationId) })
      closeDialog()
    },
    onError: mutationError,
  })
  const activeMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => setEmployeeActive(organizationId, id, isActive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: administrationKeys.employees(organizationId) }),
    onError: (error) => setActionError(t(administrationErrorKey(error))),
  })

  const employees = useMemo(
    () => employeesQuery.data ?? [],
    [employeesQuery.data],
  )
  const filteredEmployees = useMemo(() => {
    const value = search.trim().toLocaleLowerCase()
    if (!value) return employees
    return employees.filter((employee) => `${employee.code} ${employee.displayName}`.toLocaleLowerCase().includes(value))
  }, [employees, search])

  function submitEmployee(form: HTMLFormElement) {
    const data = new FormData(form)
    const code = String(data.get('code') ?? '').trim()
    const displayName = String(data.get('displayName') ?? '').trim()
    setFormError(null)
    if (dialog?.type === 'edit') updateMutation.mutate({ id: dialog.employee.id, code, displayName })
    else createMutation.mutate({ code, displayName })
  }

  function toggleActive(employee: Employee) {
    setActionError(null)
    const nextActive = !employee.isActive
    const message = nextActive ? t('administration.confirm.activateEmployee') : t('administration.confirm.deactivateEmployee')
    if (window.confirm(message.replace('{name}', employee.displayName))) {
      activeMutation.mutate({ id: employee.id, isActive: nextActive })
    }
  }

  if (organizationsQuery.isLoading || employeesQuery.isLoading) return <AdministrationLoading label={t('administration.loading')} />
  if (organizationsQuery.isError || employeesQuery.isError) {
    const error = organizationsQuery.error ?? employeesQuery.error
    return <AdministrationError label={t(administrationErrorKey(error))} onRetry={() => { void organizationsQuery.refetch(); void employeesQuery.refetch() }} retryLabel={t('administration.retry')} />
  }
  if (!organization) {
    return <AdministrationError label={t('administration.error.notFound')} onRetry={() => void organizationsQuery.refetch()} retryLabel={t('administration.retry')} />
  }

  const pending = createMutation.isPending || updateMutation.isPending
  return (
    <div>
      <Link className="mb-4 inline-flex min-h-11 items-center gap-2 rounded-lg text-sm font-semibold text-blue-950 hover:underline" to="/administration/organizations">
        <AppIcon className="size-5" name="arrowBack" />{t('administration.backToOrganizations')}
      </Link>
      <div className="mb-6 rounded-xl border border-slate-300 bg-white p-5">
        <div className="flex flex-wrap items-center gap-2">
          <h3 className="text-xl font-bold">{organization.name}</h3>
          <StatusBadge active={organization.isActive} activeLabel={t('administration.active')} inactiveLabel={t('administration.inactive')} />
        </div>
        <p className="mt-1 font-mono text-sm text-slate-600">{organization.code}</p>
      </div>
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <label className="relative block w-full sm:max-w-sm">
          <span className="sr-only">{t('administration.searchEmployees')}</span>
          <AppIcon className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-500" name="search" />
          <input aria-label={t('administration.searchEmployees')} className="h-12 w-full rounded-lg border border-slate-300 bg-white pl-12 pr-4 outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20" onChange={(event) => setSearch(event.target.value)} placeholder={t('administration.searchEmployees')} type="search" value={search} />
        </label>
        <button className="flex min-h-12 items-center justify-center gap-2 rounded-lg bg-blue-950 px-4 font-semibold text-white hover:bg-blue-900" disabled={!organization.isActive} onClick={() => setDialog({ type: 'create' })} type="button">
          <AppIcon name="personAdd" />{t('administration.addEmployee')}
        </button>
      </div>
      {actionError && <p className="mb-4 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">{actionError}</p>}
      {filteredEmployees.length === 0 ? (
        <div className="grid min-h-52 place-items-center rounded-xl border border-slate-300 bg-white text-slate-600">{t('administration.noEmployees')}</div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-300 bg-white">
          <ul className="divide-y divide-slate-200">
            {filteredEmployees.map((employee) => (
              <li className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between" key={employee.id}>
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-semibold">{employee.displayName}</p>
                    <StatusBadge active={employee.isActive} activeLabel={t('administration.active')} inactiveLabel={t('administration.inactive')} />
                  </div>
                  <p className="mt-1 font-mono text-sm text-slate-600">{employee.code}</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <button className="admin-action-button" onClick={() => setDialog({ type: 'edit', employee })} type="button"><AppIcon className="size-5" name="edit" />{t('administration.edit')}</button>
                  <button className="admin-action-button" disabled={activeMutation.isPending || !organization.isActive} onClick={() => toggleActive(employee)} type="button"><AppIcon className="size-5" name={employee.isActive ? 'block' : 'check'} />{employee.isActive ? t('administration.deactivate') : t('administration.activate')}</button>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      {dialog && (
        <AdministrationDialog closeLabel={t('administration.close')} onClose={closeDialog} title={t(dialog.type === 'create' ? 'administration.createEmployee' : 'administration.editEmployee')}>
          <AdministrationForm cancelLabel={t('administration.cancel')} error={formError} isPending={pending} onCancel={closeDialog} onSubmit={submitEmployee} pendingLabel={t('administration.saving')} submitLabel={t('administration.save')}>
            <FormField defaultValue={dialog.type === 'edit' ? dialog.employee.code : ''} label={t('administration.code')} maxLength={32} minLength={2} name="code" pattern="[\p{L}\p{N}_-]+" />
            <FormField defaultValue={dialog.type === 'edit' ? dialog.employee.displayName : ''} label={t('administration.displayName')} maxLength={120} minLength={2} name="displayName" />
          </AdministrationForm>
        </AdministrationDialog>
      )}
    </div>
  )
}
