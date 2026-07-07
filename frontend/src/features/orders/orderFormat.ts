import type { AppLanguage } from '../../app/i18n/i18nContext'
import type { TranslationKey } from '../../app/i18n/translations'

export function orderLocale(language: AppLanguage) {
  return language === 'es' ? 'es-ES' : 'pl-PL'
}

export function isPublishedOrder(status: number | string) {
  return status === 1 || status === 'ReadyForPicking'
}

export function orderStatusKey(status: number | string): TranslationKey {
  return isPublishedOrder(status) ? 'orders.status.ready' : 'orders.status.draft'
}

export function formatOrderDate(value: string, language: AppLanguage) {
  return new Intl.DateTimeFormat(orderLocale(language)).format(new Date(`${value}T00:00:00`))
}

export function formatOrderDateTime(value: string, language: AppLanguage) {
  return new Intl.DateTimeFormat(orderLocale(language), { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function formatOrderNumber(value: number, language: AppLanguage) {
  return new Intl.NumberFormat(orderLocale(language), { maximumFractionDigits: 4 }).format(value)
}
