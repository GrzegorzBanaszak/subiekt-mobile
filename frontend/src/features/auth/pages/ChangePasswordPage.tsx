import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { LanguageSwitcher } from '../../../shared/components/LanguageSwitcher'
import { AuthApiError } from '../api/authApi'
import { useAuth } from '../authContext'

type ChangePasswordError = 'invalidCurrent' | 'validation' | 'mismatch' | 'unavailable'

export function ChangePasswordPage() {
  const { t } = useI18n()
  const { changePassword, passwordForRequiredChange } = useAuth()
  const navigate = useNavigate()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<ChangePasswordError | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const formData = new FormData(event.currentTarget)
    const currentPassword = String(formData.get('currentPassword') ?? '')
    const newPassword = String(formData.get('newPassword') ?? '')
    const confirmation = String(formData.get('confirmation') ?? '')

    if (newPassword !== confirmation) {
      setError('mismatch')
      return
    }

    setIsSubmitting(true)
    setError(null)
    try {
      await changePassword({ currentPassword, newPassword })
      navigate('/', { replace: true })
    } catch (requestError) {
      if (requestError instanceof AuthApiError && requestError.status === 401) {
        setError('invalidCurrent')
      } else if (requestError instanceof AuthApiError && requestError.status === 400) {
        setError('validation')
      } else {
        setError('unavailable')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="relative flex min-h-dvh items-center justify-center bg-slate-50 px-4 py-16 text-slate-950">
      <div className="absolute right-4 top-4 sm:right-6 sm:top-6">
        <LanguageSwitcher />
      </div>
      <section className="w-full max-w-lg rounded-xl border border-slate-300 bg-white px-6 py-10 shadow-lg sm:px-10">
        <h1 className="text-3xl font-bold text-blue-950">{t('changePassword.title')}</h1>
        <p className="mt-3 text-slate-700">{t('changePassword.description')}</p>

        <form className="mt-8 flex flex-col gap-5" onSubmit={handleSubmit}>
          {error && (
            <div className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">
              {t(`changePassword.error.${error}`)}
            </div>
          )}
          <PasswordField autoComplete="current-password" defaultValue={passwordForRequiredChange ?? ''} label={t('changePassword.current')} name="currentPassword" disabled={isSubmitting} />
          <PasswordField autoComplete="new-password" label={t('changePassword.new')} name="newPassword" disabled={isSubmitting} minLength={12} />
          <PasswordField autoComplete="new-password" label={t('changePassword.confirmation')} name="confirmation" disabled={isSubmitting} minLength={12} />
          <button className="mt-2 h-14 rounded-md bg-blue-950 px-4 font-semibold text-white hover:bg-blue-900 disabled:cursor-wait disabled:opacity-70" disabled={isSubmitting} type="submit">
            {t(isSubmitting ? 'changePassword.submitting' : 'changePassword.submit')}
          </button>
        </form>
      </section>
    </main>
  )
}

function PasswordField({ label, name, disabled, autoComplete, minLength, defaultValue }: {
  label: string
  name: string
  disabled: boolean
  autoComplete: string
  minLength?: number
  defaultValue?: string
}) {
  return (
    <label className="flex flex-col gap-2 font-semibold">
      {label}
      <input
        autoComplete={autoComplete}
        className="h-14 rounded-md border border-slate-300 bg-slate-50 px-4 font-normal outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20"
        disabled={disabled}
        defaultValue={defaultValue}
        minLength={minLength}
        name={name}
        required
        type="password"
      />
    </label>
  )
}
