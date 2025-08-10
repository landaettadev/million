'use client';

import { withAdminAuth } from '../../../../src/lib/auth/AdminAuthContext';
import { Building2, Users, DollarSign, TrendingUp } from 'lucide-react';

function DashboardPage() {
  const kpis = [
    { title: 'Total Properties', value: '127', change: '+12%', icon: Building2, changeType: 'positive' as const },
    { title: 'Active Listings', value: '89', change: '+5%', icon: TrendingUp, changeType: 'positive' as const },
    { title: 'Total Owners', value: '64', change: '+3%', icon: Users, changeType: 'positive' as const },
    { title: 'Revenue (MTD)', value: '$2.4M', change: '+18%', icon: DollarSign, changeType: 'positive' as const },
  ];

  const recentActivity = [
    { id: 1, action: 'New property added', property: 'Luxury Villa Miami', time: '2 hours ago' },
    { id: 2, action: 'Property sold', property: 'Downtown Condo', time: '4 hours ago' },
    { id: 3, action: 'Owner updated', property: 'Beachfront House', time: '6 hours ago' },
    { id: 4, action: 'Price changed', property: 'Modern Apartment', time: '1 day ago' },
  ];

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-white">Dashboard</h1>
        <p className="text-gray-400 mt-2">Welcome back! Here's what's happening with your properties.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {kpis.map((kpi) => (
          <div key={kpi.title} className="bg-neutral-900 border border-white/10 rounded-xl p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-gray-400 text-sm font-medium">{kpi.title}</p>
                <p className="text-2xl font-bold text-white mt-2">{kpi.value}</p>
                <p className={`text-sm font-medium mt-1 ${kpi.changeType === 'positive' ? 'text-green-400' : 'text-red-400'}`}>
                  {kpi.change} from last month
                </p>
              </div>
              <div className="bg-white/10 p-3 rounded-lg">
                <kpi.icon className="h-6 w-6 text-white" />
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="bg-neutral-900 border border-white/10 rounded-xl">
        <div className="px-6 py-4 border-b border-white/10">
          <h2 className="text-lg font-semibold text-white">Recent Activity</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-white/5">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Action</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Property</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Time</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/10">
              {recentActivity.map((activity) => (
                <tr key={activity.id} className="hover:bg-white/5">
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-300">{activity.action}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-white">{activity.property}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-400">{activity.time}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default withAdminAuth(DashboardPage);



