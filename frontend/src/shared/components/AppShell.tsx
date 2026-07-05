import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useI18n } from '../../app/i18n/i18nContext'
import { AppIcon, type AppIconName } from './AppIcon'
import { LanguageSwitcher } from './LanguageSwitcher'
import { UserIdentity } from './UserIdentity'
import { useAuth } from '../../features/auth/authContext'
import { identityManagePermission } from '../../features/administration/permissions'
import { SignOutButton } from '../../features/auth/components/SignOutButton'

interface NavigationItem {
  labelKey:
    | 'navigation.products'
    | 'navigation.orders'
    | 'navigation.picking'
    | 'navigation.pallets'
    | 'navigation.administration'
  icon: AppIconName
  to: string
  available: boolean
  permission?: string
}

const navigationItems: NavigationItem[] = [
  { labelKey: 'navigation.products', icon: 'box', to: '/products', available: true },
  { labelKey: 'navigation.orders', icon: 'cart', to: '/orders', available: false },
  { labelKey: 'navigation.picking', icon: 'clipboard', to: '/picking', available: false },
  { labelKey: 'navigation.pallets', icon: 'pallet', to: '/pallets', available: false },
  { labelKey: 'navigation.administration', icon: 'settings', to: '/administration', available: true, permission: identityManagePermission },
]

export function AppShell() {
  const { t } = useI18n()
  const { actor } = useAuth()
  const location = useLocation()
  const currentItem = navigationItems.find((item) => location.pathname.startsWith(item.to))
  const visibleNavigationItems = navigationItems.filter(
    (item) => !item.permission || actor?.permissions.includes(item.permission),
  )

  return (
    <div className="min-h-dvh bg-slate-50 text-slate-950">
      <aside className="fixed inset-y-0 left-0 z-40 hidden w-[280px] flex-col border-r border-slate-300 bg-slate-100 p-5 lg:flex">
        <UserIdentity />
        <nav aria-label={t('navigation.main')} className="mt-10 flex flex-1 flex-col gap-2">
          {visibleNavigationItems.map((item) =>
            item.available ? (
              <NavLink
                className={({ isActive }) =>
                  `flex min-h-12 items-center gap-3 rounded-lg px-3 font-semibold transition ${
                    isActive
                      ? 'bg-emerald-300 text-emerald-950'
                      : 'text-slate-800 hover:bg-slate-200'
                  }`
                }
                key={item.to}
                to={item.to}
              >
                <AppIcon name={item.icon} />
                {t(item.labelKey)}
              </NavLink>
            ) : (
              <span
                aria-disabled="true"
                className="flex min-h-12 items-center gap-3 rounded-lg px-3 font-semibold text-slate-500"
                key={item.to}
              >
                <AppIcon name={item.icon} />
                {t(item.labelKey)}
              </span>
            ),
          )}
        </nav>
      </aside>

      <div className="flex min-h-dvh flex-col lg:ml-[280px]">
        <header className="sticky top-0 z-30 flex min-h-16 items-center justify-between gap-4 border-b border-slate-300 bg-white px-4 sm:px-6 lg:px-8">
          <div>
            <span className="text-lg font-bold text-blue-950 lg:hidden">Subiekt Mobile</span>
            <h1 className="hidden text-2xl font-bold tracking-tight lg:block">
              {t(currentItem?.labelKey ?? 'navigation.products')}
            </h1>
          </div>
          <div className="flex items-center gap-2">
            <LanguageSwitcher />
            <SignOutButton />
          </div>
        </header>

        {actor?.organizationId && (
          <section
            aria-label={t('navigation.currentIdentity')}
            className="border-b border-slate-300 bg-slate-100 px-4 py-3 sm:px-6 lg:hidden"
          >
            <UserIdentity />
          </section>
        )}

        <main className="flex-1 px-4 py-6 pb-24 sm:px-6 lg:px-8 lg:pb-8">
          <Outlet />
        </main>
      </div>

      <nav
        aria-label={t('navigation.main')}
        className={`fixed inset-x-0 bottom-0 z-40 grid h-[72px] border-t border-slate-300 bg-white px-2 pb-[env(safe-area-inset-bottom)] lg:hidden ${visibleNavigationItems.length > 4 ? 'grid-cols-5' : 'grid-cols-4'}`}
      >
        {visibleNavigationItems.map((item) =>
          item.available ? (
            <NavLink
              className={({ isActive }) =>
                `my-2 flex flex-col items-center justify-center rounded-xl text-xs font-semibold ${
                  isActive ? 'bg-blue-950 text-white' : 'text-slate-700'
                }`
              }
              key={item.to}
              to={item.to}
            >
              <AppIcon className="mb-1 size-5" name={item.icon} />
              {t(item.labelKey)}
            </NavLink>
          ) : (
            <span
              aria-disabled="true"
              className="my-2 flex flex-col items-center justify-center rounded-xl text-xs font-semibold text-slate-400"
              key={item.to}
            >
              <AppIcon className="mb-1 size-5" name={item.icon} />
              {t(item.labelKey)}
            </span>
          ),
        )}
      </nav>
    </div>
  )
}
