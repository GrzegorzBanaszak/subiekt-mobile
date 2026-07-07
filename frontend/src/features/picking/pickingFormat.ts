import type { AppLanguage } from '../../app/i18n/i18nContext'
import type { TranslationKey } from '../../app/i18n/translations'

export function enumIs(value: number | string, numeric: number, text: string) {
  return value === numeric || value === text
}

export function pickingStatusKey(value: number | string): TranslationKey {
  if (enumIs(value, 0, 'Waiting')) return 'picking.status.waiting'
  if (enumIs(value, 1, 'InProgress')) return 'picking.status.inProgress'
  return 'picking.status.completed'
}

export function pickingStatusClass(value: number | string) {
  if (enumIs(value, 0, 'Waiting')) return 'bg-slate-200 text-slate-700'
  if (enumIs(value, 1, 'InProgress')) return 'bg-indigo-100 text-indigo-900'
  return 'bg-emerald-100 text-emerald-800'
}

export function pickingLocale(language: AppLanguage) {
  return language === 'es' ? 'es-ES' : 'pl-PL'
}

export function formatDate(value: string, locale = 'pl-PL') {
  return new Intl.DateTimeFormat(locale).format(new Date(`${value}T00:00:00`))
}

export function formatDateTime(value: string, locale = 'pl-PL') {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

export function formatQuantity(value: number, locale = 'pl-PL') {
  return new Intl.NumberFormat(locale, { maximumFractionDigits: 4 }).format(value)
}
