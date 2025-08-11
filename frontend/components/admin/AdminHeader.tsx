'use client';

import { usePathname } from 'next/navigation';
import { ChevronRight } from 'lucide-react';

const routeNames: Record<string, string> = {
  '/admin/dashboard': 'Dashboard',
  '/admin/properties': 'Properties',
  '/admin/owners': 'Owners', 
  '/admin/analytics': 'Analytics',
  '/admin/settings': 'Settings',
};

export default function AdminHeader() {
  const pathname = usePathname();
  
  // Get current page name from pathname
  const currentPage = routeNames[pathname] || 'Admin';

  // Generate breadcrumb items
  const generateBreadcrumbs = () => {
    const segments = pathname.split('/').filter(Boolean);
    const breadcrumbs = [{ name: 'Admin', href: '/admin/dashboard' }];
    if (segments[0] === 'admin' && segments.length > 1) {
      const currentPath = `/admin/${segments[1]}`;
      const pageName = routeNames[currentPath];
      if (pageName) breadcrumbs.push({ name: pageName, href: currentPath });
    }
    
    return breadcrumbs;
  };

  const breadcrumbs = generateBreadcrumbs();

  return (
    <header className="sticky top-0 z-30 bg-black border-b border-white/10 px-6 py-4">
      <div className="flex items-center justify-between">
        {/* Breadcrumb */}
        <div className="flex items-center space-x-2">
          <nav aria-label="Breadcrumb" className="flex items-center space-x-2">
            {breadcrumbs.map((crumb, index) => (
              <div key={`${crumb.href}-${index}`} className="flex items-center">
                {index > 0 && (
                  <ChevronRight className="w-4 h-4 text-gray-400 mx-2" />
                )}
                <span 
                  className={`text-sm font-medium ${
                    index === breadcrumbs.length - 1 
                      ? 'text-white' 
                      : 'text-gray-400 hover:text-gray-300'
                  }`}
                >
                  {crumb.name}
                </span>
              </div>
            ))}
          </nav>
        </div>

        {/* Current page title */}
        <div className="hidden sm:block">
          <h1 className="text-lg font-semibold text-white">{currentPage}</h1>
        </div>

        {/* Actions area (placeholder for future features) */}
        <div className="flex items-center space-x-4">
          {/* Placeholder for notifications, user menu, etc. */}
          <div className="w-8 h-8 bg-white/10 rounded-full flex items-center justify-center">
            <span className="text-xs text-gray-400">•••</span>
          </div>
        </div>
      </div>
    </header>
  );
}
