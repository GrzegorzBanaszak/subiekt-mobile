import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useI18n } from '../../app/i18n/i18nContext'
import { AppIcon, type AppIconName } from './AppIcon'
import { LanguageSwitcher } from './LanguageSwitcher'
import { UserIdentity } from './UserIdentity'
import { useAuth } from '../../features/auth/authContext'
import { identityManagePermission } from '../../features/administration/permissions'
import { SignOutButton } from '../../features/auth/components/SignOutButton'

type NavigationLabelKey =
    | 'navigation.products'
    | 'navigation.orders'
    | 'navigation.picking'
    | 'navigation.pallets'
    | 'navigation.administration'
    | 'navigation.customers'
    | 'navigation.customerOrders'
    | 'navigation.shipments'

interface NavigationItem {
  labelKey: NavigationLabelKey
  icon: AppIconName
  to?: string
  permission?: string
  planned?: boolean
  mobilePrimary?: boolean
}

const navigationItems: NavigationItem[] = [
  { labelKey: 'navigation.products', icon: 'box', to: '/products', mobilePrimary: true },
  { labelKey: 'navigation.orders', icon: 'cart', to: '/warehouse-orders', permission: 'warehouse-orders.manage', mobilePrimary: true },
  { labelKey: 'navigation.picking', icon: 'clipboard', to: '/picking', permission: 'warehouse-orders.read-published', mobilePrimary: true },
  { labelKey: 'navigation.pallets', icon: 'pallet', to: '/pallets', permission: 'pallets.manage', mobilePrimary: true },
  { labelKey: 'navigation.customers', icon: 'organization', planned: true },
  { labelKey: 'navigation.customerOrders', icon: 'cart', planned: true },
  { labelKey: 'navigation.shipments', icon: 'truck', planned: true },
  { labelKey: 'navigation.administration', icon: 'settings', to: '/administration', permission: identityManagePermission },
]

export function AppShell() {
  const { t } = useI18n()
  const { actor } = useAuth()
  const location = useLocation()
  const [isMobileMoreOpen, setIsMobileMoreOpen] = useState(false)
  const currentItem = navigationItems.find((item) => item.to && location.pathname.startsWith(item.to))
  const visibleNavigationItems = navigationItems.filter(
    (item) => !item.permission || actor?.permissions.includes(item.permission),
  )
  const mobilePrimaryItems = visibleNavigationItems.filter((item) => item.mobilePrimary)
  const mobileMoreItems = visibleNavigationItems.filter((item) => !item.mobilePrimary)

  return (
    <div className="min-h-dvh bg-slate-50 text-slate-950">
      <aside className="fixed inset-y-0 left-0 z-40 hidden w-[280px] flex-col border-r border-slate-300 bg-slate-100 p-5 lg:flex">
        <UserIdentity />
        <nav aria-label={t('navigation.main')} className="mt-10 flex flex-1 flex-col gap-2">
          {visibleNavigationItems.map((item) =>
            item.to ? (
              <NavLink
                className={({ isActive }) =>
                  `flex min-h-12 items-center gap-3 rounded-lg px-3 font-semibold transition ${
                    isActive
                      ? 'bg-emerald-300 text-emerald-950'
                      : 'text-slate-800 hover:bg-slate-200'
                  }`
                }
                key={item.labelKey}
                to={item.to}
              >
                <AppIcon name={item.icon} />
                {t(item.labelKey)}
              </NavLink>
            ) : (
              <span
                aria-disabled="true"
                className="flex min-h-12 items-center justify-between gap-3 rounded-lg px-3 font-semibold text-slate-500"
                data-testid={`planned-navigation-${item.labelKey}`}
                key={item.labelKey}
              >
                <span className="flex items-center gap-3"><AppIcon name={item.icon} />{t(item.labelKey)}</span>
                <small className="rounded bg-slate-200 px-2 py-1 text-xs font-semibold text-slate-600">{t('navigation.planned')}</small>
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

      {isMobileMoreOpen && (
        <div
          aria-label={t('navigation.more')}
          className="fixed inset-x-3 bottom-[80px] z-50 rounded-xl border border-slate-300 bg-white p-2 shadow-xl lg:hidden"
          id="mobile-more-menu"
        >
          {mobileMoreItems.map((item) => item.to ? (
            <NavLink
              className="flex min-h-12 items-center gap-3 rounded-lg px-3 font-semibold text-slate-800 hover:bg-slate-100"
              key={item.labelKey}
              onClick={() => setIsMobileMoreOpen(false)}
              to={item.to}
            >
              <AppIcon name={item.icon} />
              {t(item.labelKey)}
            </NavLink>
          ) : (
            <span aria-disabled="true" className="flex min-h-12 items-center justify-between gap-3 rounded-lg px-3 font-semibold text-slate-500" key={item.labelKey}>
              <span className="flex items-center gap-3"><AppIcon name={item.icon} />{t(item.labelKey)}</span>
              <small className="rounded bg-slate-200 px-2 py-1 text-xs font-semibold text-slate-600">{t('navigation.planned')}</small>
            </span>
          ))}
        </div>
      )}

      <nav
        aria-label={t('navigation.main')}
        className="fixed inset-x-0 bottom-0 z-40 grid h-[72px] border-t border-slate-300 bg-white px-2 pb-[env(safe-area-inset-bottom)] lg:hidden"
        style={{ gridTemplateColumns: `repeat(${mobilePrimaryItems.length + 1}, minmax(0, 1fr))` }}
      >
        {mobilePrimaryItems.map((item) =>
          item.to && (
            <NavLink
              className={({ isActive }) =>
                `my-2 flex flex-col items-center justify-center rounded-xl text-xs font-semibold ${
                  isActive ? 'bg-blue-950 text-white' : 'text-slate-700'
                }`
              }
              key={item.labelKey}
              to={item.to}
            >
              <AppIcon className="mb-1 size-5" name={item.icon} />
              {t(item.labelKey)}
            </NavLink>
          ),
        )}
        <button
          aria-controls="mobile-more-menu"
          aria-expanded={isMobileMoreOpen}
          className={`my-2 flex flex-col items-center justify-center rounded-xl text-xs font-semibold ${isMobileMoreOpen ? 'bg-blue-950 text-white' : 'text-slate-700'}`}
          onClick={() => setIsMobileMoreOpen((open) => !open)}
          type="button"
        >
          <AppIcon className="mb-1 size-5" name="more" />
          {t('navigation.more')}
        </button>
      </nav>
    </div>
  )
}
