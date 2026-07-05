import type { PropsWithChildren } from 'react'
import { AppIcon } from '../../../shared/components/AppIcon'

interface AdministrationDialogProps extends PropsWithChildren {
  title: string
  closeLabel: string
  onClose: () => void
}

export function AdministrationDialog({
  title,
  closeLabel,
  onClose,
  children,
}: AdministrationDialogProps) {
  return (
    <div className="fixed inset-0 z-50 grid place-items-end bg-slate-950/60 sm:place-items-center sm:p-4">
      <section
        aria-labelledby="administration-dialog-title"
        aria-modal="true"
        className="max-h-[95dvh] w-full overflow-y-auto rounded-t-2xl bg-white p-5 shadow-xl sm:max-w-lg sm:rounded-xl sm:p-6"
        role="dialog"
      >
        <div className="mb-5 flex items-center justify-between gap-4">
          <h2 className="text-xl font-bold" id="administration-dialog-title">
            {title}
          </h2>
          <button
            aria-label={closeLabel}
            className="flex size-11 items-center justify-center rounded-lg text-slate-600 hover:bg-slate-100"
            onClick={onClose}
            type="button"
          >
            <AppIcon name="close" />
          </button>
        </div>
        {children}
      </section>
    </div>
  )
}
