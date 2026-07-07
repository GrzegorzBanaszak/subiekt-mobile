import type { PickingItem } from './api/pickingApi'
import type { TranslationKey } from '../../app/i18n/translations'

export type PickingTab = 'all' | 'available' | 'mine' | 'packed'

export function isEnum(value: number | string, numeric: number, text: string) {
  return value === numeric || value === text
}

export function isSharedPicking(value: number | string) {
  return isEnum(value, 1, 'SharedTeam')
}

export function filterPickingItems(items: PickingItem[], tab: PickingTab, actorId?: string) {
  if (tab === 'available') return items.filter((item) => isEnum(item.status, 0, 'ToPick'))
  if (tab === 'mine') return items.filter((item) => item.reservedBy?.id === actorId)
  if (tab === 'packed') return items.filter((item) =>
    isEnum(item.status, 2, 'Packed') || isEnum(item.status, 3, 'AssignedToPallet'))
  return items
}

export function pickingItemStatusKey(value: number | string): TranslationKey {
  if (isEnum(value, 0, 'ToPick')) return 'picking.item.toPick'
  if (isEnum(value, 1, 'Picking')) return 'picking.item.picking'
  if (isEnum(value, 2, 'Packed')) return 'picking.item.packed'
  return 'picking.item.onPallet'
}

export function pickingItemStatusClass(value: number | string) {
  if (isEnum(value, 0, 'ToPick')) return 'bg-slate-100 text-slate-700'
  if (isEnum(value, 1, 'Picking')) return 'bg-amber-100 text-amber-900'
  if (isEnum(value, 2, 'Packed')) return 'bg-emerald-100 text-emerald-800'
  return 'bg-indigo-100 text-indigo-900'
}

export function pickingActionKey(value: number | string): TranslationKey {
  if (isEnum(value, 0, 'Reserved')) return 'picking.history.reserved'
  if (isEnum(value, 1, 'Released')) return 'picking.history.released'
  if (isEnum(value, 2, 'Packed')) return 'picking.history.packed'
  return 'picking.history.undone'
}
