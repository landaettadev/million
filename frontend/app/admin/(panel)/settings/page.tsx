'use client';

import { withAdminAuth } from '../../../../src/lib/auth/AdminAuthContext';
import { User, Shield, Bell, Palette } from 'lucide-react';

function SettingsPage() {
  const settingsSections = [
    {
      title: 'Profile Settings',
      description: 'Manage your account information and preferences',
      icon: User,
      items: [
        { label: 'Full Name', value: 'Admin User', editable: true },
        { label: 'Email', value: 'admin@millionluxury.com', editable: false },
        { label: 'Phone', value: '+1 (555) 123-4567', editable: true },
      ]
    },
    {
      title: 'Security Settings',
      description: 'Control your account security and access',
      icon: Shield,
      items: [
        { label: 'Two-Factor Authentication', value: 'Enabled', editable: true },
        { label: 'Password', value: '••••••••', editable: true },
        { label: 'Session Timeout', value: '8 hours', editable: true },
      ]
    },
    {
      title: 'Notification Preferences',
      description: 'Customize how you receive notifications',
      icon: Bell,
      items: [
        { label: 'Email Notifications', value: 'Enabled', editable: true },
        { label: 'SMS Notifications', value: 'Disabled', editable: true },
        { label: 'Property Alerts', value: 'Enabled', editable: true },
      ]
    },
    {
      title: 'Display Settings',
      description: 'Personalize your admin interface',
      icon: Palette,
      items: [
        { label: 'Theme', value: 'Dark', editable: true },
        { label: 'Language', value: 'English', editable: true },
        { label: 'Time Zone', value: 'UTC-5', editable: true },
      ]
    }
  ];

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-white">Settings</h1>
        <p className="text-gray-400 mt-2">Manage your account preferences and system settings.</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {settingsSections.map((section) => (
          <div key={section.title} className="bg-neutral-900 border border-white/10 rounded-xl p-6">
            <div className="flex items-center space-x-3 mb-4">
              <div className="bg-white/10 p-2 rounded-lg">
                <section.icon className="h-5 w-5 text-white" />
              </div>
              <div>
                <h3 className="text-lg font-semibold text-white">{section.title}</h3>
                <p className="text-sm text-gray-400">{section.description}</p>
              </div>
            </div>
            
            <div className="space-y-3">
              {section.items.map((item) => (
                <div key={item.label} className="flex items-center justify-between py-2 border-b border-white/5 last:border-b-0">
                  <span className="text-sm text-gray-300">{item.label}</span>
                  <div className="flex items-center space-x-2">
                    <span className="text-sm text-white">{item.value}</span>
                    {item.editable && (
                      <button className="text-xs text-blue-400 hover:text-blue-300 transition-colors">
                        Edit
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      <div className="bg-neutral-900 border border-white/10 rounded-xl p-6">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-semibold text-white">System Information</h3>
            <p className="text-sm text-gray-400 mt-1">Current version and system details</p>
          </div>
          <div className="text-right">
            <p className="text-sm text-gray-400">Version</p>
            <p className="text-white font-medium">1.0.0</p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default withAdminAuth(SettingsPage); 


