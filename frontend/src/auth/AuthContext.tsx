import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import * as authApi from '../api/auth';
import { clearAuth, getStoredAuth, storeAuth } from '../api/client';
import type { User } from '../types';

interface StoredAuth {
  accessToken: string;
  user: User;
}

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, name: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(
    () => (getStoredAuth() as StoredAuth | null)?.user ?? null,
  );

  const login = useCallback(async (email: string, password: string) => {
    const auth = await authApi.login(email, password);
    storeAuth({ accessToken: auth.accessToken, user: auth.user });
    setUser(auth.user);
  }, []);

  const register = useCallback(async (email: string, name: string, password: string) => {
    const auth = await authApi.register(email, name, password);
    storeAuth({ accessToken: auth.accessToken, user: auth.user });
    setUser(auth.user);
  }, []);

  const logout = useCallback(() => {
    clearAuth();
    setUser(null);
  }, []);

  const value = useMemo(
    () => ({ user, isAuthenticated: user !== null, login, register, logout }),
    [user, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside an AuthProvider.');
  }
  return context;
}
