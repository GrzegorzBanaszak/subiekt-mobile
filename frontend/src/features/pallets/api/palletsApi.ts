import type { components } from '../../../api/schema'
import { ApiRequestError } from '../../../api/apiError'
import { apiClient } from '../../../api/client'
import { getCsrfHeader } from '../../../api/csrf'

export type PalletCandidates = components['schemas']['PalletCandidatesDto']
export type PalletCandidateItem = components['schemas']['PalletCandidateItemDto']
export type PalletListItem = components['schemas']['PalletListItemDto']
export type PalletListPage = components['schemas']['PagedResultOfPalletListItemDto']
export type PalletDetails = components['schemas']['PalletDetailsDto']
export type PalletDetailsItem = components['schemas']['PalletDetailsItemDto']
export type PalletLabelIssueMode = components['schemas']['PalletLabelIssueMode']
export type PalletLabelLanguage = components['schemas']['PalletLabelLanguage']

export interface CreatePalletItem {
  orderItemId: string
  quantity: number
  itemVersion: number
}

function requireData<T>(data: T | undefined, response: Response, error?: unknown): T {
  if (!response.ok || !data) {
    const detail = typeof error === 'object' && error !== null && 'detail' in error
      && typeof error.detail === 'string' ? error.detail : undefined
    throw new ApiRequestError(response.status, detail)
  }
  return data
}

export async function getPalletCandidates(orderId: string): Promise<PalletCandidates> {
  const { data, error, response } = await apiClient.GET('/api/orders/{orderId}/pallets/candidates', {
    params: { path: { orderId } },
  })
  return requireData(data, response, error)
}

export async function getPallets(page: number, pageSize = 20): Promise<PalletListPage> {
  const { data, error, response } = await apiClient.GET('/api/pallets', {
    params: { query: { page, pageSize } },
  })
  return requireData(data, response, error)
}

export async function createPallet(orderId: string, emptyPalletWeightKg: number,
  items: CreatePalletItem[]): Promise<PalletDetails> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/orders/{orderId}/pallets', {
    params: { path: { orderId } },
    headers,
    body: {
      operationId: crypto.randomUUID(),
      emptyPalletWeightKg,
      items,
    },
  })
  return requireData(data, response, error)
}

export async function getPalletDetails(palletId: string): Promise<PalletDetails> {
  const { data, error, response } = await apiClient.GET('/api/pallets/{palletId}', {
    params: { path: { palletId } },
  })
  return requireData(data, response, error)
}

function requirePdf(data: Blob | undefined, response: Response, error?: unknown): Blob {
  if (!response.ok || !data) {
    const detail = typeof error === 'object' && error !== null && 'detail' in error
      && typeof error.detail === 'string' ? error.detail : undefined
    throw new ApiRequestError(response.status, detail)
  }
  return data
}

export async function getPalletLabelPreview(palletId: string, language: PalletLabelLanguage): Promise<Blob> {
  const { data, error, response } = await apiClient.GET('/api/pallets/{palletId}/label-preview', {
    params: { path: { palletId }, query: { language } },
    parseAs: 'blob',
  })
  return requirePdf(data, response, error)
}

export async function issuePalletLabel(palletId: string, mode: PalletLabelIssueMode,
  language: PalletLabelLanguage): Promise<Blob> {
  const headers = await getCsrfHeader()
  const { data, error, response } = await apiClient.POST('/api/pallets/{palletId}/label-issues', {
    params: { path: { palletId } },
    headers,
    body: { mode, language },
    parseAs: 'blob',
  })
  return requirePdf(data, response, error)
}
