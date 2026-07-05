import { useEffect, useMemo, useState, type PropsWithChildren } from 'react'
import {
  I18nContext,
  type AppLanguage,
  type I18nContextValue,
} from './i18nContext'
import {
  polishTranslations,
  spanishTranslations,
  type TranslationKey,
} from './translations'

const languageStorageKey = 'subiekt-mobile-language'

function getInitialLanguage(): AppLanguage {
  const storedLanguage = localStorage.getItem(languageStorageKey)
  return storedLanguage === 'es' ? 'es' : 'pl'
}

export function I18nProvider({ children }: PropsWithChildren) {
  const [language, setLanguage] = useState<AppLanguage>(getInitialLanguage)

  useEffect(() => {
    localStorage.setItem(languageStorageKey, language)
    document.documentElement.lang = language
  }, [language])

  const value = useMemo<I18nContextValue>(() => {
    const translations =
      language === 'es' ? spanishTranslations : polishTranslations

    return {
      language,
      setLanguage,
      t: (key: TranslationKey) => translations[key],
    }
  }, [language])

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}
