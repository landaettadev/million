'use client';

import { withAdminAuth } from '../../../../src/lib/auth/AdminAuthContext';
import { Plus, Search, Filter, Eye, Edit, Trash2 } from 'lucide-react';

function PropertiesPage() {
  const properties = [
    { id: '1', name: 'Luxury Penthouse Downtown', address: '123 Main St, Miami, FL', price: '$2,500,000', type: 'Sale', status: 'Active', owner: 'John Doe', beds: 3, baths: 2, sqft: '2,500' },
    { id: '2', name: 'Modern Villa Ocean View', address: '456 Ocean Dr, Miami Beach, FL', price: '$4,200,000', type: 'Sale', status: 'Active', owner: 'Jane Smith', beds: 5, baths: 4, sqft: '4,200' },
    { id: '3', name: 'Contemporary Condo', address: '789 Biscayne Blvd, Miami, FL', price: '$850,000', type: 'Sale', status: 'Sold', owner: 'Mike Johnson', beds: 2, baths: 2, sqft: '1,200' },
  ];

  return (
    <div className="space-y-8">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-white">Properties</h1>
          <p className="text-gray-400 mt-2">Manage your property listings</p>
        </div>
        <button className="mt-4 sm:mt-0 inline-flex items-center px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100 font-medium transition-colors">
          <Plus className="w-4 h-4 mr-2" />
          New Property
        </button>
      </div>

      <div className="flex flex-col sm:flex-row gap-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
          <input type="text" placeholder="Search properties..." className="w-full pl-10 pr-4 py-2 bg-neutral-900 border border-white/10 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-white/20" />
        </div>
        <div className="relative">
          <Filter className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
          <select className="pl-10 pr-8 py-2 bg-neutral-900 border border-white/10 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-white/20 appearance-none">
            <option>All Status</option>
            <option>Active</option>
            <option>Sold</option>
            <option>Pending</option>
          </select>
        </div>
      </div>

      <div className="bg-neutral-900 border border-white/10 rounded-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-white/5">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Property</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Price</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Details</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Owner</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/10">
              {properties.map((property) => (
                <tr key={property.id} className="hover:bg-white/5">
                  <td className="px-6 py-4">
                    <div>
                      <div className="text-sm font-medium text-white">{property.name}</div>
                      <div className="text-sm text-gray-400">{property.address}</div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-white">{property.price}</div>
                    <div className="text-sm text-gray-400">{property.type}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-300">{property.beds} bed, {property.baths} bath</div>
                    <div className="text-sm text-gray-400">{property.sqft} sq ft</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-300">{property.owner}</td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${property.status === 'Active' ? 'bg-green-100 text-green-800' : property.status === 'Sold' ? 'bg-gray-100 text-gray-800' : 'bg-yellow-100 text-yellow-800'}`}>{property.status}</span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-400">
                    <div className="flex items-center gap-2">
                      <button className="text-gray-400 hover:text-white" title="View"><Eye className="w-4 h-4" /></button>
                      <button className="text-gray-400 hover:text-white" title="Edit"><Edit className="w-4 h-4" /></button>
                      <button className="text-gray-400 hover:text-red-400" title="Delete"><Trash2 className="w-4 h-4" /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default withAdminAuth(PropertiesPage);



