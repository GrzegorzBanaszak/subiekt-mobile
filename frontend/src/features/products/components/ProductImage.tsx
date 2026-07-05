import { useState } from 'react'
import { AppIcon } from '../../../shared/components/AppIcon'

export function ProductImage({ imageUrl, name }: { imageUrl: string | null; name: string }) {
  const [hasError, setHasError] = useState(false)

  if (!imageUrl || hasError) {
    return (
      <span className="flex size-12 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-400">
        <AppIcon className="size-5" name="image" />
      </span>
    )
  }

  return (
    <img
      alt=""
      className="size-12 shrink-0 rounded-lg bg-slate-100 object-contain"
      onError={() => setHasError(true)}
      src={imageUrl}
      title={name}
    />
  )
}
