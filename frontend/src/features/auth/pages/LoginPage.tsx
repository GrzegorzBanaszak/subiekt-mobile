import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useI18n } from '../../../app/i18n/i18nContext'
import { LanguageSwitcher } from '../../../shared/components/LanguageSwitcher'
import { AppIcon, type AppIconName } from '../../../shared/components/AppIcon'
import { AuthApiError } from '../api/authApi'
import { useAuth } from '../authContext'
import { OrganizationSignInForm } from '../components/OrganizationSignInForm'

type LoginError = 'invalidCredentials' | 'tooManyAttempts' | 'unavailable'

function getLoginError(error: unknown): LoginError {
  if (error instanceof AuthApiError) {
    if (error.status === 401) return 'invalidCredentials'
    if (error.status === 429) return 'tooManyAttempts'
  }

  return 'unavailable'
}

function Icon({ name }: { name: Extract<AppIconName, 'box' | 'user' | 'lock' | 'login' | 'error'> }) {
  return <AppIcon name={name} />
}

export function LoginPage() {
  const { t } = useI18n()
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [loginError, setLoginError] = useState<LoginError | null>(null)
  const [loginMode, setLoginMode] = useState<'administrator' | 'employee'>('administrator')

  function navigateAfterSignIn() {
    const requestedPath = (location.state as { from?: unknown } | null)?.from
    navigate(typeof requestedPath === 'string' ? requestedPath : '/', {
      replace: true,
    })
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setLoginError(null)

    const formData = new FormData(event.currentTarget)

    try {
      await signIn({
        username: String(formData.get('username') ?? '').trim(),
        password: String(formData.get('password') ?? ''),
      })

      navigateAfterSignIn()
    } catch (error) {
      setLoginError(getLoginError(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="relative flex min-h-dvh items-center justify-center bg-white px-4 py-24 text-slate-950">
      <div className="absolute right-4 top-4 sm:right-6 sm:top-6">
        <LanguageSwitcher />
      </div>

      <section className="w-full max-w-[500px] rounded-xl border border-slate-300 bg-white px-6 py-10 shadow-[0_12px_24px_rgba(15,23,42,0.12)] sm:px-10 sm:py-12">
        <div className="mb-10 flex flex-col items-center text-center">
          <div className="mb-7 flex size-20 items-center justify-center rounded-md border border-blue-950 bg-blue-900 text-blue-300 shadow-sm">
            <Icon name="box" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight text-blue-950 sm:text-4xl">
            {t('login.title')}
          </h1>
          <p className="mt-4 text-base text-slate-700 sm:text-lg">
            {t('login.subtitle')}
          </p>
        </div>

        <div className="mb-6 grid grid-cols-2 rounded-lg bg-slate-100 p-1" role="tablist" aria-label={t('login.mode')}>
          <button
            aria-selected={loginMode === 'administrator'}
            className={`min-h-11 rounded-md px-3 font-semibold transition ${loginMode === 'administrator' ? 'bg-white text-blue-950 shadow-sm' : 'text-slate-600 hover:text-slate-950'}`}
            onClick={() => setLoginMode('administrator')}
            role="tab"
            type="button"
          >
            {t('login.administratorTab')}
          </button>
          <button
            aria-selected={loginMode === 'employee'}
            className={`min-h-11 rounded-md px-3 font-semibold transition ${loginMode === 'employee' ? 'bg-white text-blue-950 shadow-sm' : 'text-slate-600 hover:text-slate-950'}`}
            onClick={() => setLoginMode('employee')}
            role="tab"
            type="button"
          >
            {t('login.employeeTab')}
          </button>
        </div>

        {loginMode === 'administrator' ? <form className="flex flex-col gap-5" onSubmit={handleSubmit}>
          {loginError && (
            <div
              className="flex items-center gap-3 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800"
              role="alert"
            >
              <Icon name="error" />
              <span>{t(`login.error.${loginError}`)}</span>
            </div>
          )}

          <label className="flex flex-col gap-2 font-semibold text-slate-950">
            {t('login.username')}
            <span className="relative">
              <span className="pointer-events-none absolute inset-y-0 left-4 flex items-center text-slate-700">
                <Icon name="user" />
              </span>
              <input
                aria-label={t('login.username')}
                autoComplete="username"
                autoFocus
                className="h-14 w-full rounded-md border border-slate-300 bg-slate-50 pl-13 pr-4 font-normal text-slate-950 outline-none transition placeholder:text-slate-500 focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20"
                disabled={isSubmitting}
                name="username"
                placeholder={t('login.usernamePlaceholder')}
                required
                type="text"
              />
            </span>
          </label>

          <label className="flex flex-col gap-2 font-semibold text-slate-950">
            {t('login.password')}
            <span className="relative">
              <span className="pointer-events-none absolute inset-y-0 left-4 flex items-center text-slate-700">
                <Icon name="lock" />
              </span>
              <input
                aria-label={t('login.password')}
                autoComplete="current-password"
                className="h-14 w-full rounded-md border border-slate-300 bg-slate-50 pl-13 pr-4 font-normal text-slate-950 outline-none transition placeholder:text-slate-500 focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20"
                disabled={isSubmitting}
                name="password"
                placeholder={t('login.passwordPlaceholder')}
                required
                type="password"
              />
            </span>
          </label>

          <button
            className="mt-3 flex h-14 items-center justify-center gap-3 rounded-md bg-blue-950 px-4 font-semibold text-white transition hover:bg-blue-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-950 active:scale-[0.99] disabled:cursor-wait disabled:opacity-70"
            disabled={isSubmitting}
            type="submit"
          >
            <Icon name="login" />
            {t(isSubmitting ? 'login.submitting' : 'login.submit')}
          </button>

        </form> : <OrganizationSignInForm onSuccess={navigateAfterSignIn} />}

        <p className="mt-7 text-center text-sm text-blue-950">
          {t('login.help')}
        </p>
      </section>
    </main>
  )
}
