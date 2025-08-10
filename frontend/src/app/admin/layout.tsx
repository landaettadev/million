import { AdminAuthProvider } from '../lib/auth/AdminAuthContext';

export default function AdminRootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AdminAuthProvider>
      {children}
    </AdminAuthProvider>
  );
}
