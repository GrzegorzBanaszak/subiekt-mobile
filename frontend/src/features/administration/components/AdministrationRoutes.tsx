import { Navigate, NavLink, Outlet } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useI18n } from '../../../app/i18n/i18nContext'
import { useAuth } from '../../auth/authContext'
import {
  administratorsManagePermission,
} from '../permissions'

export function AdministrationGuard({
  permission,
  children,
}: {
  permission: string
  children: ReactNode
}) {
  const { actor } = useAuth()

  if (!actor?.permissions.includes(permission)) {
    return <Navigate replace to="/products" />
  }

  return children
}

export function AdministrationIndex() {
  const { actor } = useAuth()
  const target = actor?.permissions.includes(administratorsManagePermission)
    ? '/administration/administrators'
    : '/administration/organizations'
  return <Navigate replace to={target} />
}

export function AdministrationLayout() {
  const { actor } = useAuth()
  const { t } = useI18n()
  const canManageAdministrators = actor?.permissions.includes(
    administratorsManagePermission,
  )

  return (
    <section className="mx-auto max-w-[1400px]">
      <div className="mb-6">
        <h2 className="text-2xl font-bold tracking-tight lg:text-3xl">
          {t('administration.title')}
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          {t('administration.description')}
        </p>
      </div>
      <nav
        aria-label={t('administration.tabs')}
        className="mb-6 flex gap-2 overflow-x-auto border-b border-slate-300"
      >
        {canManageAdministrators && (
          <NavLink
            className={({ isActive }) =>
              `min-h-12 whitespace-nowrap border-b-2 px-4 py-3 font-semibold ${
                isActive
                  ? 'border-blue-950 text-blue-950'
                  : 'border-transparent text-slate-600 hover:text-slate-950'
              }`
            }
            to="/administration/administrators"
          >
            {t('administration.administrators')}
          </NavLink>
        )}
        <NavLink
          className={({ isActive }) =>
            `min-h-12 whitespace-nowrap border-b-2 px-4 py-3 font-semibold ${
              isActive
                ? 'border-blue-950 text-blue-950'
                : 'border-transparent text-slate-600 hover:text-slate-950'
            }`
          }
          to="/administration/organizations"
        >
          {t('administration.organizations')}
        </NavLink>
      </nav>
      <Outlet />
    </section>
  )
}
