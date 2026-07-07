import { describe, expect, it } from 'vitest'
import type { PickingItem } from './api/pickingApi'
import { filterPickingItems, isSharedPicking, pickingItemStatusKey } from './pickingView'

function item(id: string, status: number, reservedById?: string): PickingItem {
  return {
    id,
    productId: 1,
    productName: id,
    productSymbol: null,
    orderedQuantity: 1,
    remainingQuantity: status === 2 ? 0 : 1,
    unit: 'szt.',
    status,
    version: 1,
    reservedBy: reservedById ? { kind: 0, id: reservedById, displayName: 'Pracownik', atUtc: '2026-07-07T10:00:00Z' } : null,
    packedQuantity: null,
    packedBy: null,
    actions: { canReserve: false, canRelease: false, canPack: false, canUndoPack: false },
  }
}

describe('pickingView', () => {
  const items = [item('available', 0), item('mine', 1, 'employee-1'), item('other', 1, 'employee-2'), item('packed', 2)]

  it('uses multiple tabs only for shared picking', () => {
    expect(isSharedPicking(1)).toBe(true)
    expect(isSharedPicking('SharedTeam')).toBe(true)
    expect(isSharedPicking(0)).toBe(false)
  })

  it('filters available, current employee and packed items', () => {
    expect(filterPickingItems(items, 'available').map((x) => x.id)).toEqual(['available'])
    expect(filterPickingItems(items, 'mine', 'employee-1').map((x) => x.id)).toEqual(['mine'])
    expect(filterPickingItems(items, 'packed').map((x) => x.id)).toEqual(['packed'])
  })

  it('maps item statuses to warehouse labels', () => {
    expect(pickingItemStatusKey(0)).toBe('picking.item.toPick')
    expect(pickingItemStatusKey(1)).toBe('picking.item.picking')
    expect(pickingItemStatusKey(2)).toBe('picking.item.packed')
    expect(pickingItemStatusKey(3)).toBe('picking.item.onPallet')
  })
})
