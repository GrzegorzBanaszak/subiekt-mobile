import { ApiRequestError } from '../../api/apiError'
import type { TranslationKey } from '../../app/i18n/translations'

export function customersErrorKey(error: unknown): TranslationKey {
  if (error instanceof ApiRequestError) {
    if (error.status === 400) return 'customers.error.validation'
    if (error.status === 403) return 'customers.error.forbidden'
    if (error.status === 404) return 'customers.error.notFound'
    if (error.status === 409) return 'customers.error.conflict'
  }
  return 'customers.error.unavailable'
}
