import { useI18n } from '../../../app/i18n/i18nContext'
import type { ProductListItem } from '../api/productsApi'
import { ProductImage } from './ProductImage'

function numberValue(value: number | string | null | undefined) {
  const parsedValue = Number(value ?? 0)
  return Number.isFinite(parsedValue) ? parsedValue : 0
}

function stockValue(
  formatter: Intl.NumberFormat,
  product: ProductListItem,
  value: number | string | null | undefined,
) {
  return product.stock ? formatter.format(numberValue(value)) : '—'
}

export function ProductList({ products }: { products: ProductListItem[] }) {
  const { language, t } = useI18n()
  const formatter = new Intl.NumberFormat(language === 'pl' ? 'pl-PL' : 'es-ES', {
    maximumFractionDigits: 3,
  })

  return (
    <>
      <div className="hidden overflow-hidden rounded-xl border border-slate-300 bg-white md:block">
        <table className="w-full border-collapse text-left">
          <thead className="border-b border-slate-300 bg-slate-100 text-sm text-slate-700">
            <tr>
              <th className="px-5 py-4 font-semibold">{t('products.product')}</th>
              <th className="px-5 py-4 font-semibold">{t('products.symbol')}</th>
              <th className="px-5 py-4 text-right font-semibold">{t('products.stock')}</th>
              <th className="px-5 py-4 text-right font-semibold">{t('products.reserved')}</th>
              <th className="px-5 py-4 text-right font-semibold">{t('products.available')}</th>
              <th className="px-5 py-4 font-semibold">{t('products.unit')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200">
            {products.map((product) => {
              const name = product.name || t('products.unnamed')
              return (
                <tr className="transition hover:bg-slate-50" key={product.id}>
                  <td className="px-5 py-3">
                    <div className="flex items-center gap-3">
                      <ProductImage imageUrl={product.imageUrl} name={name} />
                      <span className="font-semibold text-slate-950">{name}</span>
                    </div>
                  </td>
                  <td className="px-5 py-3 font-mono text-sm text-slate-700">
                    {product.symbol || '—'}
                  </td>
                  <td className="px-5 py-3 text-right tabular-nums">
                    {stockValue(formatter, product, product.stock?.quantity)}
                  </td>
                  <td className="px-5 py-3 text-right tabular-nums text-slate-600">
                    {stockValue(formatter, product, product.stock?.reserved)}
                  </td>
                  <td className="px-5 py-3 text-right font-semibold tabular-nums text-emerald-800">
                    {stockValue(formatter, product, product.stock?.available)}
                  </td>
                  <td className="px-5 py-3 text-slate-700">{product.unit || '—'}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <div className="flex flex-col gap-3 md:hidden">
        {products.map((product) => {
          const name = product.name || t('products.unnamed')
          return (
            <article className="rounded-xl border border-slate-300 bg-white p-4" key={product.id}>
              <div className="flex items-start gap-3">
                <ProductImage imageUrl={product.imageUrl} name={name} />
                <div className="min-w-0">
                  <h2 className="font-semibold leading-5">{name}</h2>
                  <p className="mt-1 truncate font-mono text-xs text-slate-600">
                    {product.symbol || '—'}
                  </p>
                </div>
              </div>
              <dl className="mt-4 grid grid-cols-3 gap-3 border-t border-slate-200 pt-3">
                <div>
                  <dt className="text-xs text-slate-600">{t('products.stock')}</dt>
                  <dd className="mt-1 font-semibold tabular-nums">
                    {stockValue(formatter, product, product.stock?.quantity)}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-600">{t('products.reserved')}</dt>
                  <dd className="mt-1 font-semibold tabular-nums">
                    {stockValue(formatter, product, product.stock?.reserved)}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-600">{t('products.available')}</dt>
                  <dd className="mt-1 font-semibold tabular-nums text-emerald-800">
                    {stockValue(formatter, product, product.stock?.available)}{' '}
                    <span className="font-normal text-slate-600">{product.unit || ''}</span>
                  </dd>
                </div>
              </dl>
            </article>
          )
        })}
      </div>
    </>
  )
}
