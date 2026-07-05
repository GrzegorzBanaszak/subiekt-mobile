export const administrationKeys = {
  administrators: ['administration', 'administrators'] as const,
  organizations: ['administration', 'organizations'] as const,
  employees: (organizationId: string) =>
    ['administration', 'organizations', organizationId, 'employees'] as const,
}
