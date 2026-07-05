import { useI18n, type AppLanguage } from '../../app/i18n/i18nContext'
import { AppIcon } from './AppIcon'

export function LanguageSwitcher() {
  const { language, setLanguage, t } = useI18n()

  return (
    <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
      <span className="sr-only">{t('language.label')}</span>
      <AppIcon className="size-5 text-slate-500" name="language" />
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
