import Link from 'next/link';
import { ReactNode } from 'react';

interface SidebarItemProps {
  href: string;
  icon: ReactNode;
  label: string;
  collapsed: boolean;
  active?: boolean;
  onClick?: () => void;
}

export default function SidebarItem({ 
  href, 
  icon, 
  label, 
  collapsed, 
  active = false,
  onClick 
}: SidebarItemProps) {
  const baseClasses = `
    flex items-center gap-3 rounded-xl px-3 py-2 transition-colors
    focus:outline-none focus:ring-2 focus:ring-white/20
    ${collapsed ? 'justify-center' : ''}
  `;
  
  const stateClasses = active
    ? 'bg-white/10 text-white'
    : 'text-gray-400 hover:text-white hover:bg-white/10';

  const content = (
    <>
      {icon}
      {!collapsed && (
        <span className="text-sm font-medium">{label}</span>
      )}
      {collapsed && (
        <span className="sr-only">{label}</span>
      )}
    </>
  );

  return (
    <Link
      href={href}
      className={`${baseClasses} ${stateClasses}`}
      title={collapsed ? label : undefined}
      onClick={onClick}
    >
      {content}
    </Link>
  );
}
