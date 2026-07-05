import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { useAuth } from '../authContext'

function LoadingScreen() {
  const { t } = useI18n()

  return (
    <div className="grid min-h-dvh place-items-center bg-white text-sm text-slate-600">
      <span role="status">{t('app.loading')}</span>
    </div>
  )
}

export function ProtectedRoute() {
  const { actor, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return <LoadingScreen />
  }

  if (!actor) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (actor.requiresPasswordChange && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />
  }

  if (!actor.requiresPasswordChange && location.pathname === '/change-password') {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export function GuestRoute() {
  const { actor, isLoading } = useAuth()

  if (isLoading) {
    return <LoadingScreen />
  }

  return actor ? <Navigate to="/" replace /> : <Outlet />
}
