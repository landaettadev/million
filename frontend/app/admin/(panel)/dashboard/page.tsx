'use client';

import { withAdminAuth } from '../../../../src/lib/auth/AdminAuthContext';
import { Building2, Users, DollarSign, TrendingUp } from 'lucide-react';
import { useEffect, useState } from 'react';
import { getAdminProperties, type AdminPropertyDto } from '../../../../src/lib/adminApi';

function DashboardPage() {
  const kpis = [
    { title: 'Total Properties', value: '127', change: '+12%', icon: Building2, changeType: 'positive' as const },
    { title: 'Active Listings', value: '89', change: '+5%', icon: TrendingUp, changeType: 'positive' as const },
    { title: 'Total Owners', value: '64', change: '+3%', icon: Users, changeType: 'positive' as const },
    { title: 'Revenue (MTD)', value: '$2.4M', change: '+18%', icon: DollarSign, changeType: 'positive' as const },
  ];

  const [recent, setRecent] = useState<AdminPropertyDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    const load = async () => {
      setLoading(true);
      try {
        const res = await getAdminProperties({ page: 1, pageSize: 5, sortBy: 'CreatedAt', sortDirection: 'desc' });
        if (!active) return;
        setRecent(res.items);
      } catch {
        if (!active) return;
        // Fallback mock recent list
        setRecent([
          { id: '1', ownerId: '', ownerName: '', name: 'Luxury Penthouse Downtown', address: '123 Main St, Miami, FL', price: 2500000, operationType: 'Sale', description: '', beds: 3, baths: 3, halfBaths: 1, sqft: 1800, createdAt: new Date().toISOString(), isDeleted: false },
          { id: '2', ownerId: '', ownerName: '', name: 'Modern Villa with Ocean View', address: '456 Ocean Dr, Miami Beach, FL', price: 4200000, operationType: 'Sale', description: '', beds: 4, baths: 4, halfBaths: 1, sqft: 3200, createdAt: new Date(Date.now()-86400000).toISOString(), isDeleted: false },
        ]);
      } finally {
        if (active) setLoading(false);
      }
    };
    load();
    return () => { active = false; };
  }, []);

  return (
    <div className="space-y-8">
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
        {kpis.map((kpi) => (
          <div key={kpi.title} className="bg-neutral-900 border border-white/10 rounded-xl p-6">
            <div className="text-sm text-gray-400">{kpi.title}</div>
            <div className="text-2xl font-semibold text-white mt-2">{kpi.value}</div>
            <div className={`text-xs mt-2 ${kpi.changeType === 'positive' ? 'text-emerald-400' : 'text-red-400'}`}>{kpi.change}</div>
          </div>
        ))}
      </div>

      <div className="bg-neutral-900 border border-white/10 rounded-xl p-6">
        <div className="text-sm text-gray-400 mb-3">Recent Activity</div>
        {loading ? (
          <div className="space-y-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-10 bg-neutral-800/60 rounded" />
            ))}
          </div>
        ) : (
          <ul className="divide-y divide-white/10">
            {recent.map((p) => (
              <li key={p.id} className="py-3 flex items-center justify-between">
                <div>
                  <div className="text-white font-medium">{p.name}</div>
                  <div className="text-xs text-gray-400">{p.address}</div>
                </div>
                <div className="text-sm text-gray-300">${p.price.toLocaleString()}</div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

export default withAdminAuth(DashboardPage);



