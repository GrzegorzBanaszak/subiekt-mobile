import { ApiRequestError } from '../../api/apiError'
import type { TranslationKey } from '../../app/i18n/translations'

export function administrationErrorKey(error: unknown): TranslationKey {
  if (error instanceof ApiRequestError) {
    if (error.status === 400) return 'administration.error.validation'
    if (error.status === 403) return 'administration.error.forbidden'
    if (error.status === 404) return 'administration.error.notFound'
    if (error.status === 409) return 'administration.error.conflict'
  }

  return 'administration.error.unavailable'
}
