import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { HomePage } from './HomePage'

describe('HomePage', () => {
  it('renders the application starting point', () => {
    render(<HomePage />)

    expect(
      screen.getByRole('heading', { name: 'Podgląd danych Subiekta GT' }),
    ).toBeInTheDocument()
  })
})
