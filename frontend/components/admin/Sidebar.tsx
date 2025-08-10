'use client';

import { useState, useEffect } from 'react';
import { usePathname } from 'next/navigation';
import { 
  Menu, 
  X, 
  Home, 
  Building2, 
  Users, 
  BarChart3, 
  Settings, 
  LogOut 
} from 'lucide-react';
import { useAdminAuth } from '../../src/lib/auth/AdminAuthContext';
import SidebarItem from './SidebarItem';

const navigation = [
  { href: '/admin/dashboard', label: 'Dashboard', icon: Home },
  { href: '/admin/properties', label: 'Properties', icon: Building2 },
  { href: '/admin/owners', label: 'Owners', icon: Users },
  { href: '/admin/analytics', label: 'Analytics', icon: BarChart3 },
  { href: '/admin/settings', label: 'Settings', icon: Settings },
];

export default function Sidebar() {
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const { user, logout } = useAdminAuth();
  const pathname = usePathname();

  // Load collapsed state from localStorage
  useEffect(() => {
    const saved = localStorage.getItem('sidebar-collapsed');
    if (saved !== null) {
      setIsCollapsed(JSON.parse(saved));
    }
  }, []);

  // Save collapsed state to localStorage
  useEffect(() => {
    localStorage.setItem('sidebar-collapsed', JSON.stringify(isCollapsed));
  }, [isCollapsed]);

  // Handle ESC key for mobile drawer
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isMobileOpen) {
        setIsMobileOpen(false);
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isMobileOpen]);

  // Handle focus management for mobile drawer
  useEffect(() => {
    if (isMobileOpen) {
      // Focus first navigation item when drawer opens
      const firstNavItem = document.querySelector('#sidebar-nav a');
      if (firstNavItem instanceof HTMLElement) {
        firstNavItem.focus();
      }
    }
  }, [isMobileOpen]);

  const handleToggleCollapsed = () => {
    setIsCollapsed(!isCollapsed);
  };

  const handleToggleMobile = () => {
    setIsMobileOpen(!isMobileOpen);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleToggleCollapsed();
    }
  };

  const handleLogout = () => {
    logout();
    window.location.href = '/admin/login';
  };

  return (
    <>
      {/* Mobile header with hamburger */}
      <div className="md:hidden bg-neutral-900 border-b border-white/10 px-4 py-3 flex items-center justify-between">
        <h1 className="text-xl font-bold text-white">MILLION</h1>
        <button
          onClick={handleToggleMobile}
          aria-label="Toggle sidebar"
          aria-controls="mobile-sidebar"
          aria-expanded={isMobileOpen}
          className="text-gray-400 hover:text-white focus:outline-none focus:ring-2 focus:ring-white/20 rounded p-1"
        >
          <Menu className="w-6 h-6" />
        </button>
      </div>

      {/* Mobile backdrop */}
      {isMobileOpen && (
        <div 
          className="fixed inset-0 bg-black/40 z-40 md:hidden"
          onClick={() => setIsMobileOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* Sidebar */}
      <aside
        aria-label="Admin sidebar"
        className={`
          ${isCollapsed ? 'w-16' : 'w-64'} 
          transition-[width] duration-200 ease-in-out
          bg-neutral-900 border-r border-white/10 flex flex-col
          hidden md:flex
        `}
      >
        {/* Desktop header with toggle */}
        <div className="p-4 border-b border-white/10 flex items-center justify-between">
          {!isCollapsed && (
            <h1 className="text-xl font-bold text-white">MILLION</h1>
          )}
          <button
            onClick={handleToggleCollapsed}
            onKeyDown={handleKeyDown}
            aria-label="Toggle sidebar"
            aria-controls="sidebar-nav"
            aria-expanded={!isCollapsed}
            className="text-gray-400 hover:text-white focus:outline-none focus:ring-2 focus:ring-white/20 rounded p-1"
          >
            <Menu className="w-5 h-5" />
          </button>
        </div>

        {/* Navigation */}
        <nav 
          id="sidebar-nav" 
          role="navigation" 
          className="flex-1 p-4 space-y-2"
        >
          {navigation.map((item) => (
            <SidebarItem
              key={item.href}
              href={item.href}
              icon={<item.icon className="w-5 h-5" />}
              label={item.label}
              collapsed={isCollapsed}
              active={pathname.startsWith(item.href)}
            />
          ))}
        </nav>

        {/* User section */}
        <div className="p-4 border-t border-white/10">
          {!isCollapsed && (
            <div className="flex items-center gap-3 mb-3">
              <div className="w-8 h-8 bg-white/10 rounded-full flex items-center justify-center text-white text-sm font-medium">
                {user?.name?.charAt(0) || 'A'}
              </div>
              <div className="min-w-0">
                <p className="text-sm font-medium text-white truncate">{user?.name || 'Admin'}</p>
                <p className="text-xs text-gray-400 truncate">{user?.email || 'admin@millionluxury.com'}</p>
              </div>
            </div>
          )}
          <button
            onClick={handleLogout}
            title={isCollapsed ? 'Sign out' : undefined}
            className={`
              flex items-center gap-3 w-full rounded-xl px-3 py-2 
              text-gray-400 hover:text-white hover:bg-white/10 
              transition-colors focus:outline-none focus:ring-2 focus:ring-white/20
              ${isCollapsed ? 'justify-center' : ''}
            `}
          >
            <LogOut className="w-5 h-5" />
            {!isCollapsed && <span className="text-sm">Sign out</span>}
          </button>
        </div>
      </aside>

      {/* Mobile sidebar */}
      <aside
        id="mobile-sidebar"
        role="dialog"
        aria-modal="true"
        aria-label="Navigation menu"
        className={`
          fixed inset-y-0 left-0 z-50 w-64 bg-neutral-900 transform transition-transform duration-300 ease-in-out md:hidden
          ${isMobileOpen ? 'translate-x-0' : '-translate-x-full'}
        `}
      >
        {/* Mobile header */}
        <div className="p-4 border-b border-white/10 flex items-center justify-between">
          <h1 className="text-xl font-bold text-white">MILLION</h1>
          <button
            onClick={() => setIsMobileOpen(false)}
            aria-label="Close sidebar"
            className="text-gray-400 hover:text-white focus:outline-none focus:ring-2 focus:ring-white/20 rounded p-1"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Mobile navigation */}
        <nav className="flex-1 p-4 space-y-2">
          {navigation.map((item) => (
            <SidebarItem
              key={item.href}
              href={item.href}
              icon={<item.icon className="w-5 h-5" />}
              label={item.label}
              collapsed={false}
              active={pathname.startsWith(item.href)}
              onClick={() => setIsMobileOpen(false)}
            />
          ))}
        </nav>

        {/* Mobile user section */}
        <div className="p-4 border-t border-white/10">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-8 h-8 bg-white/10 rounded-full flex items-center justify-center text-white text-sm font-medium">
              {user?.name?.charAt(0) || 'A'}
            </div>
            <div>
              <p className="text-sm font-medium text-white">{user?.name || 'Admin'}</p>
              <p className="text-xs text-gray-400">{user?.email || 'admin@millionluxury.com'}</p>
            </div>
          </div>
          <button
            onClick={handleLogout}
            className="flex items-center gap-3 w-full rounded-xl px-3 py-2 text-gray-400 hover:text-white hover:bg-white/10 transition-colors"
          >
            <LogOut className="w-5 h-5" />
            <span className="text-sm">Sign out</span>
          </button>
        </div>
      </aside>
    </>
  );
}
