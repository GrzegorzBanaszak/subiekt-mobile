import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { useAuth } from '../../auth/authContext'
import { administrationErrorKey } from '../administrationError'
import {
  createAdministrator,
  getAdministrators,
  resetAdministratorPassword,
  setAdministratorActive,
  updateAdministrator,
  type Administrator,
  type CreatedAdministrator,
} from '../api/administrationApi'
import { AdministrationDialog } from '../components/AdministrationDialog'
import { AdministrationForm, FormField } from '../components/AdministrationForm'
import { AdministrationError, AdministrationLoading, StatusBadge } from '../components/AdministrationState'
import { administrationKeys } from '../queryKeys'

type DialogState =
  | { type: 'create' }
  | { type: 'edit'; administrator: Administrator }
  | { type: 'password'; administrator: Administrator }
  | { type: 'created'; result: CreatedAdministrator }
  | null

export function AdministratorsPage() {
  const { t } = useI18n()
  const { actor, clearSession } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [dialog, setDialog] = useState<DialogState>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [passwordCopied, setPasswordCopied] = useState(false)

  const administratorsQuery = useQuery({
    queryKey: administrationKeys.administrators,
    queryFn: getAdministrators,
  })

  function closeDialog() {
    setDialog(null)
    setFormError(null)
    setPasswordCopied(false)
  }

  function mutationError(error: unknown) {
    setFormError(t(administrationErrorKey(error)))
  }

  const createMutation = useMutation({
    mutationFn: createAdministrator,
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: administrationKeys.administrators })
      setDialog({ type: 'created', result })
    },
    onError: mutationError,
  })
  const updateMutation = useMutation({
    mutationFn: ({ id, username, displayName }: { id: string; username: string; displayName: string }) =>
      updateAdministrator(id, { username, displayName }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: administrationKeys.administrators })
      closeDialog()
    },
    onError: mutationError,
  })
  const passwordMutation = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) =>
      resetAdministratorPassword(id, password),
    onSuccess: async (_, variables) => {
      closeDialog()
      if (variables.id === actor?.id) {
        clearSession()
        navigate('/login', { replace: true })
      }
    },
    onError: mutationError,
  })
  const activeMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      setAdministratorActive(id, isActive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: administrationKeys.administrators }),
    onError: (error) => setActionError(t(administrationErrorKey(error))),
  })

  function submitAdministrator(form: HTMLFormElement) {
    const data = new FormData(form)
    const username = String(data.get('username') ?? '').trim()
    const displayName = String(data.get('displayName') ?? '').trim()
    setFormError(null)

    if (dialog?.type === 'edit') {
      updateMutation.mutate({ id: dialog.administrator.id, username, displayName })
      return
    }

    createMutation.mutate({ username, displayName })
  }

  function submitPassword(form: HTMLFormElement, administrator: Administrator) {
    const data = new FormData(form)
    const password = String(data.get('password') ?? '')
    const confirmation = String(data.get('passwordConfirmation') ?? '')
    setFormError(null)
    if (password !== confirmation) {
      setFormError(t('administration.error.passwordMismatch'))
      return
    }
    passwordMutation.mutate({ id: administrator.id, password })
  }

  function toggleActive(administrator: Administrator) {
    setActionError(null)
    const nextActive = !administrator.isActive
    const message = nextActive
      ? t('administration.confirm.activateAdministrator')
      : t('administration.confirm.deactivateAdministrator')
    if (window.confirm(message.replace('{name}', administrator.displayName))) {
      activeMutation.mutate({ id: administrator.id, isActive: nextActive })
    }
  }

  if (administratorsQuery.isLoading) {
    return <AdministrationLoading label={t('administration.loading')} />
  }
  if (administratorsQuery.isError) {
    return (
      <AdministrationError
        label={t(administrationErrorKey(administratorsQuery.error))}
        onRetry={() => void administratorsQuery.refetch()}
        retryLabel={t('administration.retry')}
      />
    )
  }

  const administrators = administratorsQuery.data ?? []
  const pending = createMutation.isPending || updateMutation.isPending || passwordMutation.isPending

  return (
    <div>
      <div className="mb-4 flex items-center justify-between gap-4">
        <p className="text-sm text-slate-600">
          {t('administration.administratorsCount').replace('{count}', String(administrators.length))}
        </p>
        <button
          className="flex min-h-12 items-center gap-2 rounded-lg bg-blue-950 px-4 font-semibold text-white hover:bg-blue-900"
          onClick={() => setDialog({ type: 'create' })}
          type="button"
        >
          <AppIcon name="add" />
          {t('administration.addAdministrator')}
        </button>
      </div>
      {actionError && <p className="mb-4 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">{actionError}</p>}
      <div className="overflow-hidden rounded-xl border border-slate-300 bg-white">
        <ul className="divide-y divide-slate-200">
          {administrators.map((administrator) => (
            <li className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between" key={administrator.id}>
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="font-semibold">{administrator.displayName}</p>
                  {administrator.isBootstrapAdministrator && (
                    <span className="rounded-full bg-blue-100 px-2.5 py-1 text-xs font-semibold text-blue-900">ROOT</span>
                  )}
                  <StatusBadge active={administrator.isActive} activeLabel={t('administration.active')} inactiveLabel={t('administration.inactive')} />
                </div>
                <p className="mt-1 font-mono text-sm text-slate-600">{administrator.username}</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <button className="admin-action-button" onClick={() => setDialog({ type: 'edit', administrator })} type="button"><AppIcon className="size-5" name="edit" />{t('administration.edit')}</button>
                <button className="admin-action-button" onClick={() => setDialog({ type: 'password', administrator })} type="button"><AppIcon className="size-5" name="key" />{t('administration.resetPassword')}</button>
                {!administrator.isBootstrapAdministrator && (
                  <button className="admin-action-button" disabled={activeMutation.isPending} onClick={() => toggleActive(administrator)} type="button">
                    <AppIcon className="size-5" name={administrator.isActive ? 'block' : 'check'} />
                    {administrator.isActive ? t('administration.deactivate') : t('administration.activate')}
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      </div>

      {(dialog?.type === 'create' || dialog?.type === 'edit') && (
        <AdministrationDialog closeLabel={t('administration.close')} onClose={closeDialog} title={t(dialog.type === 'create' ? 'administration.createAdministrator' : 'administration.editAdministrator')}>
          <AdministrationForm cancelLabel={t('administration.cancel')} error={formError} isPending={pending} onCancel={closeDialog} onSubmit={submitAdministrator} pendingLabel={t('administration.saving')} submitLabel={t('administration.save')}>
            <FormField autoComplete="username" defaultValue={dialog.type === 'edit' ? dialog.administrator.username : ''} label={t('administration.username')} maxLength={64} minLength={3} name="username" pattern="[\p{L}\p{N}._@-]+" />
            <FormField defaultValue={dialog.type === 'edit' ? dialog.administrator.displayName : ''} label={t('administration.displayName')} maxLength={120} minLength={2} name="displayName" />
          </AdministrationForm>
        </AdministrationDialog>
      )}

      {dialog?.type === 'created' && (
        <AdministrationDialog closeLabel={t('administration.close')} onClose={closeDialog} title={t('administration.administratorCreated')}>
          <div className="flex flex-col gap-4">
            <p className="text-sm text-slate-700">{t('administration.temporaryPasswordDescription')}</p>
            <label className="flex flex-col gap-2 text-sm font-semibold text-slate-800">
              {t('administration.temporaryPassword')}
              <input
                className="h-12 rounded-lg border border-slate-300 bg-slate-50 px-4 font-mono"
                onFocus={(event) => event.currentTarget.select()}
                readOnly
                value={dialog.result.temporaryPassword}
              />
            </label>
            <button
              className="min-h-12 rounded-lg border border-slate-300 px-5 font-semibold hover:bg-slate-100"
              onClick={async () => {
                await navigator.clipboard.writeText(dialog.result.temporaryPassword)
                setPasswordCopied(true)
              }}
              type="button"
            >
              {t(passwordCopied ? 'administration.passwordCopied' : 'administration.copyPassword')}
            </button>
            <p className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900">
              {t('administration.temporaryPasswordWarning')}
            </p>
            <button className="min-h-12 rounded-lg bg-blue-950 px-5 font-semibold text-white hover:bg-blue-900" onClick={closeDialog} type="button">
              {t('administration.close')}
            </button>
          </div>
        </AdministrationDialog>
      )}

      {dialog?.type === 'password' && (
        <AdministrationDialog closeLabel={t('administration.close')} onClose={closeDialog} title={t('administration.resetPassword')}>
          <AdministrationForm cancelLabel={t('administration.cancel')} error={formError} isPending={pending} onCancel={closeDialog} onSubmit={(form) => submitPassword(form, dialog.administrator)} pendingLabel={t('administration.saving')} submitLabel={t('administration.savePassword')}>
            <p className="text-sm text-slate-600">{dialog.administrator.displayName}</p>
            <FormField autoComplete="new-password" label={t('administration.password')} maxLength={128} minLength={12} name="password" type="password" />
            <FormField autoComplete="new-password" label={t('administration.passwordConfirmation')} maxLength={128} minLength={12} name="passwordConfirmation" type="password" />
          </AdministrationForm>
        </AdministrationDialog>
      )}
    </div>
  )
}
