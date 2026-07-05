import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import {
  getEmployees,
  getOrganizations,
} from '../../features/auth/api/authApi'
import { useAuth } from '../../features/auth/authContext'
import { useI18n } from '../../app/i18n/i18nContext'
import { AppIcon } from './AppIcon'

export function UserIdentity() {
  const { actor, switchEmployee } = useAuth()
  const { t } = useI18n()
  const [isSwitching, setIsSwitching] = useState(false)
  const [switchError, setSwitchError] = useState(false)
  const isEmployee = !!actor?.organizationId

  const organizationsQuery = useQuery({
    queryKey: ['auth', 'organizations'],
    queryFn: getOrganizations,
    enabled: isEmployee,
  })
  const employeesQuery = useQuery({
    queryKey: ['auth', 'organizations', actor?.organizationId, 'employees'],
    queryFn: () => getEmployees(actor!.organizationId!),
    enabled: isEmployee,
  })

  if (!actor) return null

  const organizationName = organizationsQuery.data?.find(
    (organization) => organization.id === actor.organizationId,
  )?.name

  async function handleEmployeeChange(employeeId: string) {
    if (!actor?.organizationId || employeeId === actor.id) return

    setIsSwitching(true)
    setSwitchError(false)
    try {
      await switchEmployee(actor.organizationId, employeeId)
    } catch {
      setSwitchError(true)
    } finally {
      setIsSwitching(false)
    }
  }

  return (
    <div className="flex min-w-0 items-center gap-3">
      <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-slate-200 text-slate-700">
        <AppIcon name="user" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate text-lg font-bold leading-6 text-blue-950">
          {isEmployee
            ? organizationName ?? t('navigation.organization')
            : actor.displayName}
        </p>
        {isEmployee ? (
          <label className="mt-1 block">
            <span className="block text-xs font-semibold uppercase tracking-wide text-slate-500">
              {t('navigation.changeEmployee')}
            </span>
            <select
              aria-invalid={switchError}
              aria-label={t('navigation.employee')}
              className="mt-1 h-10 w-full cursor-pointer rounded-lg border border-slate-300 bg-white px-3 text-sm font-semibold text-slate-800 outline-none transition hover:border-slate-400 focus-visible:border-blue-900 focus-visible:ring-2 focus-visible:ring-blue-900/20"
              disabled={isSwitching || employeesQuery.isLoading}
              onChange={(event) => void handleEmployeeChange(event.target.value)}
              value={actor.id}
            >
              {!employeesQuery.data && (
                <option value={actor.id}>{actor.displayName}</option>
              )}
              {employeesQuery.data?.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.displayName}
                </option>
              ))}
            </select>
            {switchError && (
              <span className="mt-1 block text-xs text-red-700" role="alert">
                {t('navigation.employeeSwitchError')}
              </span>
            )}
          </label>
        ) : (
          <p className="truncate text-sm text-slate-600">
            {t('navigation.administrator')}
          </p>
        )}
      </div>
    </div>
  )
}
