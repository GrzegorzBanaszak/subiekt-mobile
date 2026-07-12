import type { AppLanguage } from '../../app/i18n/i18nContext'
import type { TranslationKey } from '../../app/i18n/translations'

export function palletLocale(language: AppLanguage) {
  return language === 'es' ? 'es-ES' : 'pl-PL'
}

export function formatPalletQuantity(value: number, locale = 'pl-PL') {
  return new Intl.NumberFormat(locale, { maximumFractionDigits: 4 }).format(value)
}

export function formatWeightKg(value: number, locale = 'pl-PL') {
  return `${new Intl.NumberFormat(locale, { minimumFractionDigits: 1, maximumFractionDigits: 4 }).format(value)} kg`
}

export function palletStatusKey(value: number | string): TranslationKey {
  void value
  return 'pallets.status.closed'
}

export function palletStatusClass(value: number | string) {
  void value
  return 'bg-emerald-100 text-emerald-800'
}
