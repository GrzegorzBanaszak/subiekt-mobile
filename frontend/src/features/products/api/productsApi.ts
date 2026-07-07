import type { components } from '../../../api/schema'
import { apiClient } from '../../../api/client'

export type ProductListItem = components['schemas']['ProductListItemResponse']
export type ProductPage = components['schemas']['PagedResponseOfProductListItemResponse']
export type ProductDetails = components['schemas']['ProductDetailsResponse']

export class ProductsApiError extends Error {
  constructor(public readonly status: number) {
    super(`Products request failed with status ${status}.`)
  }
}

export async function getProducts({
  search,
  page,
  pageSize,
}: {
  search: string
  page: number
  pageSize: number
}): Promise<ProductPage> {
  const { data, response } = await apiClient.GET('/api/products', {
    params: {
      query: {
        Search: search || undefined,
        Page: page,
        PageSize: pageSize,
      },
    },
  })

  if (!response.ok || !data) {
    throw new ProductsApiError(response.status)
  }

  return data
}

export async function getProductDetails(id: number): Promise<ProductDetails> {
  const { data, response } = await apiClient.GET('/api/products/{id}', {
    params: { path: { id } },
  })

  if (!response.ok || !data) {
    throw new ProductsApiError(response.status)
  }

  return data
}
