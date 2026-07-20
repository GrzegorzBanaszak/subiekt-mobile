import type { AppLanguage } from '../../app/i18n/i18nContext'
import type { TranslationKey } from '../../app/i18n/translations'

export function warehouseOrderLocale(language: AppLanguage) {
  return language === 'es' ? 'es-ES' : 'pl-PL'
}

export function isPublishedWarehouseOrder(status: number | string) {
  return status === 1 || status === 'ReadyForPicking'
}

export function warehouseOrderStatusKey(status: number | string): TranslationKey {
  return isPublishedWarehouseOrder(status) ? 'orders.status.ready' : 'orders.status.draft'
}

export function formatWarehouseOrderDate(value: string, language: AppLanguage) {
  return new Intl.DateTimeFormat(warehouseOrderLocale(language)).format(new Date(`${value}T00:00:00`))
}

export function formatWarehouseOrderDateTime(value: string, language: AppLanguage) {
  return new Intl.DateTimeFormat(warehouseOrderLocale(language), { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function formatWarehouseOrderNumber(value: number, language: AppLanguage) {
  return new Intl.NumberFormat(warehouseOrderLocale(language), { maximumFractionDigits: 4 }).format(value)
}
