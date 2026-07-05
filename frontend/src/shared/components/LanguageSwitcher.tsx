import { useI18n, type AppLanguage } from '../../app/i18n/i18nContext'

export function LanguageSwitcher() {
  const { language, setLanguage, t } = useI18n()

  return (
    <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
      <span className="sr-only">{t('language.label')}</span>
      <svg
        aria-hidden="true"
        className="size-5 text-slate-500"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
        strokeWidth="1.8"
      >
        <circle cx="12" cy="12" r="9" />
        <path d="M3 12h18M12 3c2.4 2.5 3.7 5.5 3.7 9S14.4 18.5 12 21c-2.4-2.5-3.7-5.5-3.7-9S9.6 5.5 12 3Z" />
      </svg>
      <select
        aria-label={t('language.label')}
        className="h-10 rounded-lg border border-slate-300 bg-white px-3 text-sm text-slate-800 shadow-sm outline-none transition focus:border-blue-800 focus:ring-2 focus:ring-blue-800/20"
        value={language}
        onChange={(event) => setLanguage(event.target.value as AppLanguage)}
      >
        <option value="pl">{t('language.polish')}</option>
        <option value="es">{t('language.spanish')}</option>
      </select>
    </label>
  )
}
