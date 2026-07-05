import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useMemo, type PropsWithChildren } from 'react'
import {
  getCurrentActor,
  selectEmployee,
  signOut as signOutRequest,
  signInAdministrator,
  type SignInCredentials,
} from './api/authApi'
import { AuthContext, type AuthContextValue } from './authContext'

const currentActorQueryKey = ['auth', 'current-actor'] as const

export function AuthProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient()
  const currentActorQuery = useQuery({
    queryKey: currentActorQueryKey,
    queryFn: getCurrentActor,
    retry: false,
  })

  const signIn = useCallback(
    async (credentials: SignInCredentials) => {
      const actor = await signInAdministrator(credentials)
      queryClient.setQueryData(currentActorQueryKey, actor)
    },
    [queryClient],
  )

  const switchEmployee = useCallback(
    async (organizationId: string, employeeId: string) => {
      const actor = await selectEmployee(organizationId, employeeId)
      queryClient.setQueryData(currentActorQueryKey, actor)
    },
    [queryClient],
  )

  const clearSession = useCallback(() => {
    queryClient.setQueryData(currentActorQueryKey, null)
  }, [queryClient])

  const signOut = useCallback(async () => {
    await signOutRequest()
    queryClient.setQueryData(currentActorQueryKey, null)
    queryClient.removeQueries({ queryKey: ['auth'], exact: false })
  }, [queryClient])

  const value = useMemo<AuthContextValue>(
    () => ({
      actor: currentActorQuery.data ?? null,
      isLoading: currentActorQuery.isLoading,
      signIn,
      switchEmployee,
      signOut,
      clearSession,
    }),
    [clearSession, currentActorQuery.data, currentActorQuery.isLoading, signIn, signOut, switchEmployee],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
