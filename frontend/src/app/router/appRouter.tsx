import { createBrowserRouter, Navigate } from 'react-router-dom'
import {
  GuestRoute,
  ProtectedRoute,
} from '../../features/auth/components/AuthRoute'
import { LoginPage } from '../../features/auth/pages/LoginPage'
import { ProductsPage } from '../../features/products/pages/ProductsPage'
import { AdministratorsPage } from '../../features/administration/pages/AdministratorsPage'
import { OrganizationsPage } from '../../features/administration/pages/OrganizationsPage'
import { OrganizationDetailsPage } from '../../features/administration/pages/OrganizationDetailsPage'
import {
  AdministrationGuard,
  AdministrationIndex,
  AdministrationLayout,
} from '../../features/administration/components/AdministrationRoutes'
import {
  administratorsManagePermission,
  identityManagePermission,
} from '../../features/administration/permissions'
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
            element: <Navigate replace to="/products" />,
          },
          {
            path: '/products',
            element: <ProductsPage />,
          },
          {
            path: '/administration',
            element: (
              <AdministrationGuard permission={identityManagePermission}>
                <AdministrationLayout />
              </AdministrationGuard>
            ),
            children: [
              { index: true, element: <AdministrationIndex /> },
              {
                path: 'administrators',
                element: (
                  <AdministrationGuard permission={administratorsManagePermission}>
                    <AdministratorsPage />
                  </AdministrationGuard>
                ),
              },
              { path: 'organizations', element: <OrganizationsPage /> },
              {
                path: 'organizations/:organizationId',
                element: <OrganizationDetailsPage />,
              },
            ],
          },
        ],
      },
    ],
  },
])
