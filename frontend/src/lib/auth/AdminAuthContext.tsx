'use client';

import { createContext, useContext, useState, useEffect, ReactNode } from 'react';

interface AdminUser {
  id: string;
  email: string;
  name: string;
  role: 'admin';
}

interface AdminAuthContextType {
  user: AdminUser | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<boolean>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AdminAuthContext = createContext<AdminAuthContextType | undefined>(undefined);

// Mock admin credentials
const MOCK_ADMIN = {
  email: 'admin@millionluxury.com',
  password: 'admin123',
  user: {
    id: '1',
    email: 'admin@millionluxury.com',
    name: 'Admin User',
    role: 'admin' as const,
  }
};

interface AdminAuthProviderProps {
  children: ReactNode;
}

export function AdminAuthProvider({ children }: AdminAuthProviderProps) {
  const [user, setUser] = useState<AdminUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Check for existing session on mount
  useEffect(() => {
    const savedUser = localStorage.getItem('admin_user');
    if (savedUser) {
      try {
        setUser(JSON.parse(savedUser));
      } catch (error) {
        console.error('Error parsing saved user:', error);
        localStorage.removeItem('admin_user');
      }
    }
    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string): Promise<boolean> => {
    setIsLoading(true);
    
    // Simulate API call delay
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    const fullEmail = email.includes('@') ? email : `${email}@millionluxury.com`;
    
    if (fullEmail === MOCK_ADMIN.email && password === MOCK_ADMIN.password) {
      setUser(MOCK_ADMIN.user);
      localStorage.setItem('admin_user', JSON.stringify(MOCK_ADMIN.user));
      setIsLoading(false);
      return true;
    }
    
    setIsLoading(false);
    return false;
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('admin_user');
  };

  const value: AdminAuthContextType = {
    user,
    isLoading,
    login,
    logout,
    isAuthenticated: !!user,
  };

  return (
    <AdminAuthContext.Provider value={value}>
      {children}
    </AdminAuthContext.Provider>
  );
}

export function useAdminAuth() {
  const context = useContext(AdminAuthContext);
  if (context === undefined) {
    throw new Error('useAdminAuth must be used within an AdminAuthProvider');
  }
  return context;
}

// HOC for protecting admin routes
export function withAdminAuth<P extends object>(
  Component: React.ComponentType<P>
) {
  return function ProtectedComponent(props: P) {
    const { isAuthenticated, isLoading } = useAdminAuth();

    if (isLoading) {
      return (
        <div className="min-h-screen flex items-center justify-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-gray-900"></div>
        </div>
      );
    }

    if (!isAuthenticated) {
      if (typeof window !== 'undefined') {
        window.location.href = '/admin/login';
      }
      return null;
    }

    return <Component {...props} />;
  };
}
