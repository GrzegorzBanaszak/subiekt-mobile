import { useState } from 'react'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { useAuth } from '../authContext'

export function SignOutButton() {
  const { signOut } = useAuth()
  const { t } = useI18n()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [hasError, setHasError] = useState(false)

  async function handleSignOut() {
    setIsSubmitting(true)
    setHasError(false)
    try {
      await signOut()
    } catch {
      setHasError(true)
      setIsSubmitting(false)
    }
  }

  return (
    <div className="flex items-center gap-2">
      {hasError && (
        <span className="hidden text-sm text-red-700 sm:inline" role="alert">
          {t('logout.error')}
        </span>
      )}
      <button
        aria-label={t(isSubmitting ? 'logout.submitting' : 'logout.submit')}
        className="inline-flex min-h-11 items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 font-semibold text-slate-800 transition hover:bg-slate-100 disabled:cursor-wait disabled:opacity-60"
        disabled={isSubmitting}
        onClick={() => void handleSignOut()}
        type="button"
      >
        <AppIcon className="size-5" name="logout" />
        <span className="hidden sm:inline">
          {t(isSubmitting ? 'logout.submitting' : 'logout.submit')}
        </span>
      </button>
    </div>
  )
}
