import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { AuthProvider } from '../../features/auth/AuthProvider'
import { I18nProvider } from '../i18n/I18nProvider'
import { appRouter } from '../router/appRouter'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})

export function AppProviders() {
  return (
    <I18nProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RouterProvider router={appRouter} />
        </AuthProvider>
      </QueryClientProvider>
    </I18nProvider>
  )
}
