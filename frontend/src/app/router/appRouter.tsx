import { createBrowserRouter } from 'react-router-dom'
import {
  GuestRoute,
  ProtectedRoute,
} from '../../features/auth/components/AuthRoute'
import { LoginPage } from '../../features/auth/pages/LoginPage'
import { HomePage } from '../../shared/pages/HomePage'
import { AppShell } from '../../shared/components/AppShell'

export const appRouter = createBrowserRouter([
  {
    element: <GuestRoute />,
    children: [
      {
        path: '/login',
        element: <LoginPage />,
      },
    ],
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppShell />,
        children: [
          {
            path: '/',
            element: <HomePage />,
          },
        ],
      },
    ],
  },
])
