import { useI18n } from '../../app/i18n/i18nContext'

export function HomePage() {
  const { t } = useI18n()

  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <p className="text-sm font-medium text-blue-700">{t('home.eyebrow')}</p>
      <h1 className="mt-2 text-3xl font-bold tracking-tight">
        {t('home.title')}
      </h1>
      <p className="mt-3 max-w-2xl text-slate-600">
        {t('home.description')}
      </p>
    </section>
  )
}
