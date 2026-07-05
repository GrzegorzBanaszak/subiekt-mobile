import { createContext, useContext } from 'react'
import type { TranslationKey } from './translations'

export type AppLanguage = 'pl' | 'es'

export interface I18nContextValue {
  language: AppLanguage
  setLanguage: (language: AppLanguage) => void
  t: (key: TranslationKey) => string
}

export const I18nContext = createContext<I18nContextValue | null>(null)

export function useI18n() {
  const context = useContext(I18nContext)

  if (!context) {
    throw new Error('useI18n musi być użyty wewnątrz I18nProvider.')
  }

  return context
}
