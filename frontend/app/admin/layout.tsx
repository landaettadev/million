import { AdminAuthProvider } from '../../src/lib/auth/AdminAuthContext';

export default function AdminRootLayout({ children }: { children: React.ReactNode }) {
  return <AdminAuthProvider>{children}</AdminAuthProvider>;
}