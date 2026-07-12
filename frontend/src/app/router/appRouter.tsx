import { createBrowserRouter, Navigate } from 'react-router-dom'
import {
  GuestRoute,
  ProtectedRoute,
} from '../../features/auth/components/AuthRoute'
import { LoginPage } from '../../features/auth/pages/LoginPage'
import { ChangePasswordPage } from '../../features/auth/pages/ChangePasswordPage'
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
import { OrdersPage } from '../../features/orders/pages/OrdersPage'
import { NewOrderPage } from '../../features/orders/pages/NewOrderPage'
import { OrderDetailsPage } from '../../features/orders/pages/OrderDetailsPage'
import { PickingOrdersPage } from '../../features/picking/pages/PickingOrdersPage'
import { PickingOrderPage } from '../../features/picking/pages/PickingOrderPage'
import { PalletsPage } from '../../features/pallets/pages/PalletsPage'
import { NewPalletPage } from '../../features/pallets/pages/NewPalletPage'
import { PalletDetailsPage } from '../../features/pallets/pages/PalletDetailsPage'

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
        path: '/change-password',
        element: <ChangePasswordPage />,
      },
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
            path: '/orders',
            element: <AdministrationGuard permission="orders.manage"><OrdersPage /></AdministrationGuard>,
          },
          {
            path: '/orders/new',
            element: <AdministrationGuard permission="orders.manage"><NewOrderPage /></AdministrationGuard>,
          },
          {
            path: '/orders/:orderId',
            element: <AdministrationGuard permission="orders.manage"><OrderDetailsPage /></AdministrationGuard>,
          },
          {
            path: '/picking',
            element: <AdministrationGuard permission="orders.read-published"><PickingOrdersPage /></AdministrationGuard>,
          },
          {
            path: '/picking/:orderId',
            element: <AdministrationGuard permission="orders.read-published"><PickingOrderPage /></AdministrationGuard>,
          },
          {
            path: '/pallets',
            element: <AdministrationGuard permission="pallets.manage"><PalletsPage /></AdministrationGuard>,
          },
          {
            path: '/picking/:orderId/pallets/new',
            element: <AdministrationGuard permission="pallets.manage"><NewPalletPage /></AdministrationGuard>,
          },
          {
            path: '/pallets/:palletId',
            element: <AdministrationGuard permission="pallets.manage"><PalletDetailsPage /></AdministrationGuard>,
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
