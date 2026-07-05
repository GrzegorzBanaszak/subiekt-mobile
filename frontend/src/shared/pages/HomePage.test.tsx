import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { I18nProvider } from '../../app/i18n/I18nProvider'
import { HomePage } from './HomePage'

describe('HomePage', () => {
  it('renders the application starting point', () => {
    render(
      <I18nProvider>
        <HomePage />
      </I18nProvider>,
    )

    expect(
      screen.getByRole('heading', { name: 'Podgląd danych Subiekta GT' }),
    ).toBeInTheDocument()
  })
})
