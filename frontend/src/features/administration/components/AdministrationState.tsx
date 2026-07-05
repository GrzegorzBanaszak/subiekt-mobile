import { AppIcon } from '../../../shared/components/AppIcon'

export function AdministrationLoading({ label }: { label: string }) {
  return <div className="grid min-h-52 place-items-center text-slate-600" role="status">{label}</div>
}

export function AdministrationError({ label, retryLabel, onRetry }: { label: string; retryLabel: string; onRetry: () => void }) {
  return (
    <div className="grid min-h-52 place-items-center rounded-xl border border-red-300 bg-red-50 p-6 text-center">
      <div>
        <AppIcon className="mx-auto mb-3 size-8 text-red-700" name="error" />
        <p className="text-red-900" role="alert">{label}</p>
        <button className="mt-4 min-h-11 rounded-lg bg-red-800 px-5 font-semibold text-white" onClick={onRetry} type="button">
          {retryLabel}
        </button>
      </div>
    </div>
  )
}

export function StatusBadge({ active, activeLabel, inactiveLabel }: { active: boolean; activeLabel: string; inactiveLabel: string }) {
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-semibold ${active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'}`}>
      <AppIcon className="size-4" name={active ? 'check' : 'block'} />
      {active ? activeLabel : inactiveLabel}
    </span>
  )
}
