import { describe, expect, it } from 'vitest'
import { polishTranslations, spanishTranslations } from './translations'

describe('translations', () => {
  it('provides every interface key in Polish and Spanish', () => {
    expect(Object.keys(spanishTranslations).sort()).toEqual(Object.keys(polishTranslations).sort())
    expect(Object.values(polishTranslations).every(Boolean)).toBe(true)
    expect(Object.values(spanishTranslations).every(Boolean)).toBe(true)
  })

  it('contains Spanish translations for order pages', () => {
    expect(spanishTranslations['orders.list.title']).toBe('Lista de pedidos de almacén')
    expect(spanishTranslations['orders.new.saveDraft']).toBe('Guardar borrador')
    expect(spanishTranslations['orders.details.publish']).toBe('Publicar para preparación')
  })

  it('distinguishes warehouse orders from Subiekt customer orders', () => {
    expect(polishTranslations['navigation.orders']).toBe('Zamówienia magazynowe')
    expect(polishTranslations['navigation.customerOrders']).toBe('Zamówienia od klientów')
    expect(spanishTranslations['navigation.orders']).toBe('Pedidos de almacén')
    expect(spanishTranslations['navigation.customerOrders']).toBe('Pedidos de clientes de Subiekt')
  })
})
