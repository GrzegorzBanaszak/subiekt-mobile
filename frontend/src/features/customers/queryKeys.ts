export const customerKeys = {
  all: ['customers'] as const,
  list: (search: string, active: boolean | undefined, page: number) => ['customers', 'list', { search, active, page }] as const,
  detail: (customerId: string) => ['customers', 'detail', customerId] as const,
  sites: (customerId: string, search: string, page: number) => ['customers', 'sites', customerId, { search, page }] as const,
  site: (customerId: string, siteId: string) => ['customers', 'site', customerId, siteId] as const,
  activity: (customerId: string) => ['customers', 'activity', customerId] as const,
  contractors: (search: string) => ['customers', 'contractors', search] as const,
}
