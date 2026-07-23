import { createBrowserRouter, Navigate, redirect } from 'react-router-dom'
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
  customerOrdersManagePermission,
  customersManagePermission,
  identityManagePermission,
} from '../../features/administration/permissions'
import { AppShell } from '../../shared/components/AppShell'
import { WarehouseOrdersPage } from '../../features/warehouse-orders/pages/WarehouseOrdersPage'
import { NewWarehouseOrderPage } from '../../features/warehouse-orders/pages/NewWarehouseOrderPage'
import { WarehouseOrderDetailsPage } from '../../features/warehouse-orders/pages/WarehouseOrderDetailsPage'
import { PickingOrdersPage } from '../../features/picking/pages/PickingOrdersPage'
import { PickingOrderPage } from '../../features/picking/pages/PickingOrderPage'
import { PalletsPage } from '../../features/pallets/pages/PalletsPage'
import { NewPalletPage } from '../../features/pallets/pages/NewPalletPage'
import { PalletDetailsPage } from '../../features/pallets/pages/PalletDetailsPage'
import { CustomersPage } from '../../features/customers/pages/CustomersPage'
import { CustomerFormPage } from '../../features/customers/pages/CustomerFormPage'
import { CustomerDetailsPage } from '../../features/customers/pages/CustomerDetailsPage'
import { CustomerSiteProfilePage } from '../../features/customers/pages/CustomerSiteProfilePage'
import { CustomerPackagingPage } from '../../features/customers/pages/CustomerPackagingPage'
import { PackagingTypesPage } from '../../features/packaging/pages/PackagingTypesPage'
import { CustomerOrdersPage } from '../../features/customer-orders/pages/CustomerOrdersPage'
import { CustomerOrderDetailsPage } from '../../features/customer-orders/pages/CustomerOrderDetailsPage'

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
            path: '/customers',
            element: <AdministrationGuard permission={customersManagePermission}><CustomersPage /></AdministrationGuard>,
          },
          {
            path: '/customers/new',
            element: <AdministrationGuard permission={customersManagePermission}><CustomerFormPage /></AdministrationGuard>,
          },
          {
            path: '/customers/:customerId',
            element: <AdministrationGuard permission={customersManagePermission}><CustomerDetailsPage /></AdministrationGuard>,
          },
          {
            path: '/customers/:customerId/edit',
            element: <AdministrationGuard permission={customersManagePermission}><CustomerFormPage /></AdministrationGuard>,
          },
          { path: '/customers/:customerId/packaging', element: <AdministrationGuard permission={customersManagePermission}><CustomerPackagingPage /></AdministrationGuard> },
          {
            path: '/customers/:customerId/sites/new',
            element: <AdministrationGuard permission={customersManagePermission}><CustomerSiteProfilePage /></AdministrationGuard>,
          },
          {
            path: '/customers/:customerId/sites/:siteId',
            element: <AdministrationGuard permission={customersManagePermission}><CustomerSiteProfilePage /></AdministrationGuard>,
          },
          { path: '/packaging-types', element: <AdministrationGuard permission={customersManagePermission}><PackagingTypesPage /></AdministrationGuard> },
          { path: '/customer-orders', element: <AdministrationGuard permission={customerOrdersManagePermission}><CustomerOrdersPage /></AdministrationGuard> },
          { path: '/customer-orders/:customerOrderId', element: <AdministrationGuard permission={customerOrdersManagePermission}><CustomerOrderDetailsPage /></AdministrationGuard> },
          {
            path: '/warehouse-orders',
            element: <AdministrationGuard permission="warehouse-orders.manage"><WarehouseOrdersPage /></AdministrationGuard>,
          },
          {
            path: '/warehouse-orders/new',
            element: <AdministrationGuard permission="warehouse-orders.manage"><NewWarehouseOrderPage /></AdministrationGuard>,
          },
          {
            path: '/warehouse-orders/:warehouseOrderId',
            element: <AdministrationGuard permission="warehouse-orders.manage"><WarehouseOrderDetailsPage /></AdministrationGuard>,
          },
          {
            path: '/orders',
            loader: () => redirect('/warehouse-orders'),
          },
          {
            path: '/orders/new',
            loader: () => redirect('/warehouse-orders/new'),
          },
          {
            path: '/orders/:warehouseOrderId',
            loader: ({ params }) => redirect(`/warehouse-orders/${params.warehouseOrderId}`),
          },
          {
            path: '/picking',
            element: <AdministrationGuard permission="warehouse-orders.read-published"><PickingOrdersPage /></AdministrationGuard>,
          },
          {
            path: '/picking/:warehouseOrderId',
            element: <AdministrationGuard permission="warehouse-orders.read-published"><PickingOrderPage /></AdministrationGuard>,
          },
          {
            path: '/pallets',
            element: <AdministrationGuard permission="pallets.manage"><PalletsPage /></AdministrationGuard>,
          },
          {
            path: '/picking/:warehouseOrderId/pallets/new',
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
