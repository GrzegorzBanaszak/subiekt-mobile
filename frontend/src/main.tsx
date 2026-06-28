import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { AppProviders } from './app/providers/AppProviders'
import './shared/styles/index.css'

const rootElement = document.getElementById('root')

if (!rootElement) {
  throw new Error('Nie znaleziono elementu #root.')
}

createRoot(rootElement).render(
  <StrictMode>
    <AppProviders />
  </StrictMode>,
)
