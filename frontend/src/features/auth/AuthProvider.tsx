import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useMemo, useState, type PropsWithChildren } from 'react'
import {
  getCurrentActor,
  changeOwnPassword,
  selectEmployee,
  signOut as signOutRequest,
  signInAdministrator,
  type SignInCredentials,
} from './api/authApi'
import { AuthContext, type AuthContextValue } from './authContext'

const currentActorQueryKey = ['auth', 'current-actor'] as const

export function AuthProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient()
  const [passwordForRequiredChange, setPasswordForRequiredChange] = useState<string | null>(null)
  const currentActorQuery = useQuery({
    queryKey: currentActorQueryKey,
    queryFn: getCurrentActor,
    retry: false,
  })

  const signIn = useCallback(
    async (credentials: SignInCredentials) => {
      const actor = await signInAdministrator(credentials)
      queryClient.setQueryData(currentActorQueryKey, actor)
      setPasswordForRequiredChange(actor.requiresPasswordChange ? credentials.password : null)
    },
    [queryClient],
  )

  const switchEmployee = useCallback(
    async (organizationId: string, employeeId: string) => {
      const actor = await selectEmployee(organizationId, employeeId)
      queryClient.setQueryData(currentActorQueryKey, actor)
      setPasswordForRequiredChange(null)
    },
    [queryClient],
  )

  const changePassword = useCallback(
    async (request: Parameters<typeof changeOwnPassword>[0]) => {
      await changeOwnPassword(request)
      const actor = await getCurrentActor()
      queryClient.setQueryData(currentActorQueryKey, actor)
      setPasswordForRequiredChange(null)
    },
    [queryClient],
  )

  const clearSession = useCallback(() => {
    queryClient.setQueryData(currentActorQueryKey, null)
    setPasswordForRequiredChange(null)
  }, [queryClient])

  const signOut = useCallback(async () => {
    await signOutRequest()
    queryClient.setQueryData(currentActorQueryKey, null)
    queryClient.removeQueries({ queryKey: ['auth'], exact: false })
    setPasswordForRequiredChange(null)
  }, [queryClient])

  const value = useMemo<AuthContextValue>(
    () => ({
      actor: currentActorQuery.data ?? null,
      passwordForRequiredChange,
      isLoading: currentActorQuery.isLoading,
      signIn,
      changePassword,
      switchEmployee,
      signOut,
      clearSession,
    }),
    [changePassword, clearSession, currentActorQuery.data, currentActorQuery.isLoading, passwordForRequiredChange, signIn, signOut, switchEmployee],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
