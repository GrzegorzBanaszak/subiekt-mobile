import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useI18n } from '../../../app/i18n/i18nContext'
import { AppIcon } from '../../../shared/components/AppIcon'
import { getProductDetails } from '../../products/api/productsApi'
import { formatQuantity, pickingLocale } from '../pickingFormat'

interface ProductDetailsDialogProps {
  productId: number
  fallbackName: string
  onClose: () => void
}

export function ProductDetailsDialog({ productId, fallbackName, onClose }: ProductDetailsDialogProps) {
  const { language, t } = useI18n()
  const [fullscreenImage, setFullscreenImage] = useState(false)
  const query = useQuery({
    queryKey: ['products', 'details', productId],
    queryFn: () => getProductDetails(productId),
  })

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      if (fullscreenImage) setFullscreenImage(false)
      else onClose()
    }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [fullscreenImage, onClose])

  const product = query.data
  const mainStock = product?.warehouses.find((warehouse) => warehouse.isMain) ?? product?.warehouses[0]
  const noData = t('picking.product.noData')
  const locale = pickingLocale(language)

  return <div className="fixed inset-0 z-50 grid place-items-end bg-slate-950/60 sm:place-items-center sm:p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
    <section aria-labelledby="product-details-title" aria-modal="true" className="max-h-[95dvh] w-full overflow-y-auto rounded-t-2xl bg-white p-5 shadow-xl sm:max-w-3xl sm:rounded-2xl sm:p-6" role="dialog">
      <header className="mb-5 flex items-center justify-between gap-4"><h2 className="text-xl font-bold text-blue-950" id="product-details-title">{t('picking.product.title')}</h2><button aria-label={t('picking.product.close')} className="grid size-11 place-items-center rounded-lg text-slate-600 hover:bg-slate-100" onClick={onClose}><AppIcon name="close" /></button></header>
      {query.isLoading ? <div className="grid min-h-72 place-items-center text-slate-600" role="status">{t('picking.product.loading')}</div>
        : query.isError || !product ? <div className="grid min-h-72 place-items-center text-center text-red-800" role="alert"><div><AppIcon className="mx-auto mb-3 size-8" name="warning" /><p className="font-semibold">{t('picking.product.error')}</p><button className="mt-4 rounded-lg bg-red-800 px-4 py-2 font-semibold text-white" onClick={() => void query.refetch()}>{t('picking.product.retry')}</button></div></div>
          : <div className="grid gap-5 sm:grid-cols-[300px_1fr]">
            <ProductPreview enlargeLabel={t('picking.product.enlargeImage')} imageUrl={product.imageUrl} name={product.name || fallbackName} noImageLabel={t('picking.product.noImage')} onExpand={() => setFullscreenImage(true)} />
            <dl className="grid content-start gap-3"><Detail label={t('picking.product.symbol')} value={product.symbol || noData} /><Detail label={t('picking.product.name')} value={product.name || fallbackName} /><Detail label={t('picking.product.description')} value={product.description || noData} /><div className="grid grid-cols-2 gap-3"><Detail label={t('picking.product.weight')} value={product.unitWeightKg == null ? noData : `${formatQuantity(Number(product.unitWeightKg), locale)} kg`} /><Detail label={t('picking.product.stock')} value={mainStock ? `${formatQuantity(Number(mainStock.quantity), locale)} ${mainStock.unit || product.unit || ''}`.trim() : noData} /></div><Detail label={t('picking.product.unit')} value={product.unit || noData} /></dl>
          </div>}
    </section>
    {fullscreenImage && product?.imageUrl && <div aria-label={t('picking.product.fullscreenImage')} aria-modal="true" className="fixed inset-0 z-[60] grid place-items-center bg-slate-950/95 p-4 sm:p-8" onMouseDown={(event) => { if (event.target === event.currentTarget) setFullscreenImage(false) }} role="dialog"><button aria-label={t('picking.product.closeFullscreen')} className="absolute right-4 top-4 grid size-12 place-items-center rounded-full bg-white/15 text-white hover:bg-white/25" onClick={() => setFullscreenImage(false)}><AppIcon name="close" /></button><img alt={product.name || fallbackName} className="max-h-full max-w-full object-contain" src={product.imageUrl} /></div>}
  </div>
}

function ProductPreview({ imageUrl, name, noImageLabel, enlargeLabel, onExpand }: { imageUrl: string | null; name: string; noImageLabel: string; enlargeLabel: string; onExpand: () => void }) {
  const [failed, setFailed] = useState(false)
  if (!imageUrl || failed) return <div className="grid min-h-72 place-items-center rounded-xl border border-slate-200 bg-slate-100 p-5 text-center text-slate-500"><div><AppIcon className="mx-auto mb-2 size-9" name="image" /><span className="text-sm">{noImageLabel}</span></div></div>
  return <button aria-label={enlargeLabel} className="group relative min-h-72 overflow-hidden rounded-xl border border-slate-200 bg-white p-2 hover:border-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-800" onClick={onExpand}><img alt={name} className="h-72 w-full object-contain transition-transform group-hover:scale-[1.03]" onError={() => setFailed(true)} src={imageUrl} /><span className="absolute bottom-3 right-3 grid size-10 place-items-center rounded-full bg-slate-950/75 text-white"><AppIcon className="size-5" name="search" /></span></button>
}

function Detail({ label, value }: { label: string; value: string }) {
  return <div className="rounded-lg bg-slate-100 p-3"><dt className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</dt><dd className="mt-1 whitespace-pre-wrap font-medium text-slate-950">{value}</dd></div>
}
