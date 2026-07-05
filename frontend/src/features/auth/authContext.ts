import { createContext, useContext } from 'react'
import type { CurrentActor, SignInCredentials } from './api/authApi'

export interface AuthContextValue {
  actor: CurrentActor | null
  isLoading: boolean
  signIn: (credentials: SignInCredentials) => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth musi być użyty wewnątrz AuthProvider.')
  }

  return context
}
