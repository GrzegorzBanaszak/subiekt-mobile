import { createContext, useContext } from 'react'
import type { ChangePasswordRequest, CurrentActor, SignInCredentials } from './api/authApi'

export interface AuthContextValue {
  actor: CurrentActor | null
  passwordForRequiredChange?: string | null
  isLoading: boolean
  signIn: (credentials: SignInCredentials) => Promise<void>
  changePassword: (request: ChangePasswordRequest) => Promise<void>
  switchEmployee: (organizationId: string, employeeId: string) => Promise<void>
  signOut: () => Promise<void>
  clearSession: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth musi być użyty wewnątrz AuthProvider.')
  }

  return context
}
