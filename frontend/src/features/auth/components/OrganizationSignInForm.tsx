import { useQuery } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { getEmployees, getOrganizations } from '../api/authApi'
import { useAuth } from '../authContext'

interface OrganizationSignInFormProps {
  onSuccess: () => void
}

export function OrganizationSignInForm({ onSuccess }: OrganizationSignInFormProps) {
  const { t } = useI18n()
  const { switchEmployee } = useAuth()
  const [organizationId, setOrganizationId] = useState('')
  const [employeeId, setEmployeeId] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [hasSignInError, setHasSignInError] = useState(false)

  const organizationsQuery = useQuery({
    queryKey: ['auth', 'organizations'],
    queryFn: getOrganizations,
    retry: false,
  })
  const employeesQuery = useQuery({
    queryKey: ['auth', 'organizations', organizationId, 'employees'],
    queryFn: () => getEmployees(organizationId),
    enabled: organizationId.length > 0,
    retry: false,
  })

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!organizationId || !employeeId) return

    setIsSubmitting(true)
    setHasSignInError(false)
    try {
      await switchEmployee(organizationId, employeeId)
      onSuccess()
    } catch {
      setHasSignInError(true)
      setIsSubmitting(false)
    }
  }

  const hasLoadingError = organizationsQuery.isError || employeesQuery.isError

  return (
    <form className="flex flex-col gap-5" onSubmit={handleSubmit}>
      {(hasLoadingError || hasSignInError) && (
        <div
          className="flex items-center gap-3 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800"
          role="alert"
        >
          <AppIcon name="error" />
          <span>{t(hasSignInError ? 'login.employee.error' : 'login.employee.loadError')}</span>
        </div>
      )}

      <label className="flex flex-col gap-2 font-semibold text-slate-950">
        {t('login.organization')}
        <select
          aria-label={t('login.organization')}
          className="h-14 w-full rounded-md border border-slate-300 bg-slate-50 px-4 font-normal text-slate-950 outline-none transition focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20"
          disabled={organizationsQuery.isLoading || isSubmitting}
          onChange={(event) => {
            setOrganizationId(event.target.value)
            setEmployeeId('')
            setHasSignInError(false)
          }}
          required
          value={organizationId}
        >
          <option value="">
            {organizationsQuery.isLoading
              ? t('login.organizationLoading')
              : t('login.organizationPlaceholder')}
          </option>
          {organizationsQuery.data?.map((organization) => (
            <option key={organization.id} value={organization.id}>
              {organization.name} ({organization.code})
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-2 font-semibold text-slate-950">
        {t('login.employee')}
        <select
          aria-label={t('login.employee')}
          className="h-14 w-full rounded-md border border-slate-300 bg-slate-50 px-4 font-normal text-slate-950 outline-none transition focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20 disabled:text-slate-500"
          disabled={!organizationId || employeesQuery.isLoading || isSubmitting}
          onChange={(event) => {
            setEmployeeId(event.target.value)
            setHasSignInError(false)
          }}
          required
          value={employeeId}
        >
          <option value="">
            {employeesQuery.isLoading
              ? t('login.employeeLoading')
              : t('login.employeePlaceholder')}
          </option>
          {employeesQuery.data?.map((employee) => (
            <option key={employee.id} value={employee.id}>
              {employee.displayName} ({employee.code})
            </option>
          ))}
        </select>
      </label>

      {organizationId && employeesQuery.data?.length === 0 && (
        <p className="text-sm text-slate-600">{t('login.employeeEmpty')}</p>
      )}

      <button
        className="mt-3 flex h-14 items-center justify-center gap-3 rounded-md bg-blue-950 px-4 font-semibold text-white transition hover:bg-blue-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-950 active:scale-[0.99] disabled:cursor-wait disabled:opacity-70"
        disabled={!organizationId || !employeeId || isSubmitting}
        type="submit"
      >
        <AppIcon name="login" />
        {t(isSubmitting ? 'login.submitting' : 'login.employeeSubmit')}
      </button>
    </form>
  )
}
