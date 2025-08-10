'use client';

import { withAdminAuth } from '../../lib/auth/AdminAuthContext';
import AdminLayout from '../../components/admin/AdminLayout';
import { Building2, Users, DollarSign, TrendingUp } from 'lucide-react';

function DashboardPage() {
  // Mock data - will be replaced with real API calls
  const stats = [
    {
      name: 'Total Properties',
      value: '127',
      change: '+12%',
      changeType: 'positive' as const,
      icon: Building2,
    },
    {
      name: 'Active Listings',
      value: '89',
      change: '+5%',
      changeType: 'positive' as const,
      icon: TrendingUp,
    },
    {
      name: 'Total Owners',
      value: '64',
      change: '+3%',
      changeType: 'positive' as const,
      icon: Users,
    },
    {
      name: 'Revenue (MTD)',
      value: '$2.4M',
      change: '+18%',
      changeType: 'positive' as const,
      icon: DollarSign,
    },
  ];

  const recentProperties = [
    {
      id: '1',
      name: 'Luxury Penthouse Downtown',
      address: '123 Main St, Miami, FL',
      price: '$2,500,000',
      status: 'For Sale',
      addedDate: '2024-01-15',
    },
    {
      id: '2',
      name: 'Modern Villa with Ocean View',
      address: '456 Ocean Dr, Miami Beach, FL',
      price: '$4,200,000',
      status: 'For Sale',
      addedDate: '2024-01-14',
    },
    {
      id: '3',
      name: 'Contemporary Condo',
      address: '789 Biscayne Blvd, Miami, FL',
      price: '$850,000',
      status: 'Sold',
      addedDate: '2024-01-13',
    },
  ];

  return (
    <AdminLayout>
      <div className="p-6 lg:p-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-serif font-bold text-gray-900">Dashboard</h1>
          <p className="mt-2 text-gray-600">Welcome back! Here's what's happening with your properties.</p>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4 mb-8">
          {stats.map((stat) => (
            <div key={stat.name} className="bg-white overflow-hidden shadow-sm rounded-lg border border-gray-200">
              <div className="p-6">
                <div className="flex items-center">
                  <div className="flex-shrink-0">
                    <stat.icon className="h-8 w-8 text-gray-900" />
                  </div>
                  <div className="ml-5 w-0 flex-1">
                    <dl>
                      <dt className="text-sm font-medium text-gray-500 truncate">
                        {stat.name}
                      </dt>
                      <dd className="flex items-baseline">
                        <div className="text-2xl font-semibold text-gray-900">
                          {stat.value}
                        </div>
                        <div className={`ml-2 flex items-baseline text-sm font-semibold ${
                          stat.changeType === 'positive' ? 'text-green-600' : 'text-red-600'
                        }`}>
                          {stat.change}
                        </div>
                      </dd>
                    </dl>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Recent Properties */}
        <div className="bg-white shadow-sm rounded-lg border border-gray-200">
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="text-lg font-medium text-gray-900">Recent Properties</h2>
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Property
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Address
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Price
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Added
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {recentProperties.map((property) => (
                  <tr key={property.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">{property.name}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-500">{property.address}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">{property.price}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                        property.status === 'For Sale' 
                          ? 'bg-green-100 text-green-800' 
                          : 'bg-gray-100 text-gray-800'
                      }`}>
                        {property.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(property.addedDate).toLocaleDateString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="px-6 py-4 border-t border-gray-200">
            <a 
              href="/admin/properties" 
              className="text-sm font-medium text-gray-900 hover:text-gray-700"
            >
              View all properties →
            </a>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
}

export default withAdminAuth(DashboardPage);
