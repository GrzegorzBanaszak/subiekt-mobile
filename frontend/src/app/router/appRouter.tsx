import { createBrowserRouter } from 'react-router-dom'
import { HomePage } from '../../shared/pages/HomePage'
import { AppShell } from '../../shared/components/AppShell'

export const appRouter = createBrowserRouter([
  {
    element: <AppShell />,
    children: [
      {
        path: '/',
        element: <HomePage />,
      },
    ],
  },
])
