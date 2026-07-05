import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useMemo, type PropsWithChildren } from 'react'
import {
  getCurrentActor,
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

  const value = useMemo<AuthContextValue>(
    () => ({
      actor: currentActorQuery.data ?? null,
      isLoading: currentActorQuery.isLoading,
      signIn,
    }),
    [currentActorQuery.data, currentActorQuery.isLoading, signIn],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
