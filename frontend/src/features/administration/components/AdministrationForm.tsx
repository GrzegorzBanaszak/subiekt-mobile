import type { FormEvent, PropsWithChildren } from 'react'

export function FormField({
  label,
  name,
  defaultValue,
  type = 'text',
  minLength,
  maxLength,
  pattern,
  autoComplete,
}: {
  label: string
  name: string
  defaultValue?: string
  type?: 'text' | 'password'
  minLength: number
  maxLength: number
  pattern?: string
  autoComplete?: string
}) {
  return (
    <label className="flex flex-col gap-2 text-sm font-semibold text-slate-800">
      {label}
      <input
        autoComplete={autoComplete}
        className="h-12 rounded-lg border border-slate-300 bg-white px-4 font-normal outline-none focus:border-blue-900 focus:ring-2 focus:ring-blue-900/20"
        defaultValue={defaultValue}
        maxLength={maxLength}
        minLength={minLength}
        name={name}
        pattern={pattern}
        required
        type={type}
      />
    </label>
  )
}

interface FormActionsProps extends PropsWithChildren {
  cancelLabel: string
  submitLabel: string
  pendingLabel: string
  isPending: boolean
  error?: string | null
  onCancel: () => void
  onSubmit: (form: HTMLFormElement) => void
}

export function AdministrationForm({
  cancelLabel,
  submitLabel,
  pendingLabel,
  isPending,
  error,
  onCancel,
  onSubmit,
  children,
}: FormActionsProps) {
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit(event.currentTarget)
  }

  return (
    <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
      {error && (
        <p className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800" role="alert">
          {error}
        </p>
      )}
      {children}
      <div className="mt-3 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button
          className="min-h-12 rounded-lg border border-slate-300 px-5 font-semibold hover:bg-slate-100"
          disabled={isPending}
          onClick={onCancel}
          type="button"
        >
          {cancelLabel}
        </button>
        <button
          className="min-h-12 rounded-lg bg-blue-950 px-5 font-semibold text-white hover:bg-blue-900 disabled:opacity-60"
          disabled={isPending}
          type="submit"
        >
          {isPending ? pendingLabel : submitLabel}
        </button>
      </div>
    </form>
  )
}
