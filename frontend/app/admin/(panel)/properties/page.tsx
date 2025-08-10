'use client';

import { withAdminAuth } from '../../../../src/lib/auth/AdminAuthContext';
import { Plus, Search, Filter, Eye, Edit, Trash2 } from 'lucide-react';
import { createProperty, deleteProperty, addImage, deleteImage } from '../../../../src/lib/adminApi';
import { useState } from 'react';

function PropertiesPage() {
  const [isCreating, setIsCreating] = useState(false);
  const [showAddImageModal, setShowAddImageModal] = useState(false);
  const [showDeleteImageModal, setShowDeleteImageModal] = useState(false);
  const [activePropertyId, setActivePropertyId] = useState<string | null>(null);
  const [imageForm, setImageForm] = useState({ url: '', order: 1, enabled: true });
  const [imageIdToDelete, setImageIdToDelete] = useState('');
  const [isSavingImage, setIsSavingImage] = useState(false);
  const [isRemovingImage, setIsRemovingImage] = useState(false);
  const [properties, setProperties] = useState([
    { id: '1', name: 'Luxury Penthouse Downtown', address: '123 Main St, Miami, FL', price: '$2,500,000', type: 'Sale', status: 'Active', owner: 'John Doe', beds: 3, baths: 2, sqft: '2,500' },
    { id: '2', name: 'Modern Villa Ocean View', address: '456 Ocean Dr, Miami Beach, FL', price: '$4,200,000', type: 'Sale', status: 'Active', owner: 'Jane Smith', beds: 5, baths: 4, sqft: '4,200' },
    { id: '3', name: 'Contemporary Condo', address: '789 Biscayne Blvd, Miami, FL', price: '$850,000', type: 'Sale', status: 'Sold', owner: 'Mike Johnson', beds: 2, baths: 2, sqft: '1,200' },
  ]);

  const handleNew = async () => {
    try {
      setIsCreating(true);
      const res = await createProperty({
        ownerId: '000000000000000000000001',
        name: 'New Property',
        address: 'Address TBD',
        price: 100000,
        operationType: 'Sale',
        beds: 2,
        baths: 1,
        halfBaths: 0,
        sqft: 900,
        description: 'Created from admin panel',
      });
      setProperties([{ id: res.id, name: 'New Property', address: 'Address TBD', price: '$100,000', type: 'Sale', status: 'Active', owner: '-', beds: 2, baths: 1, sqft: '900' }, ...properties]);
    } finally {
      setIsCreating(false);
    }
  };

  const handleDelete = async (id: string) => {
    await deleteProperty(id);
    setProperties(properties.filter(p => p.id !== id));
  };

  const openAddImage = (propertyId: string) => {
    setActivePropertyId(propertyId);
    setImageForm({ url: '', order: 1, enabled: true });
    setShowAddImageModal(true);
  };

  const handleAddImage = async () => {
    if (!activePropertyId || !imageForm.url) return;
    setIsSavingImage(true);
    try {
      await addImage({
        propertyId: activePropertyId,
        file: imageForm.url,
        enabled: imageForm.enabled,
        order: imageForm.order,
      });
      setShowAddImageModal(false);
    } finally {
      setIsSavingImage(false);
    }
  };

  const openDeleteImage = () => {
    setImageIdToDelete('');
    setShowDeleteImageModal(true);
  };

  const handleDeleteImage = async () => {
    if (!imageIdToDelete) return;
    setIsRemovingImage(true);
    try {
      await deleteImage(imageIdToDelete);
      setShowDeleteImageModal(false);
    } finally {
      setIsRemovingImage(false);
    }
  };

  return (
    <div className="space-y-8">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-white">Properties</h1>
          <p className="text-gray-400 mt-2">Manage your property listings</p>
        </div>
        <button onClick={handleNew} disabled={isCreating} className="mt-4 sm:mt-0 inline-flex items-center px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100 font-medium transition-colors disabled:opacity-50">
          <Plus className="w-4 h-4 mr-2" />
          {isCreating ? 'Creating...' : 'New Property'}
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
                      <button onClick={() => openAddImage(property.id)} className="text-gray-400 hover:text-white" title="Add image"><Plus className="w-4 h-4" /></button>
                      <button onClick={() => openDeleteImage()} className="text-gray-400 hover:text-white" title="Delete image by ID"><Trash2 className="w-4 h-4" /></button>
                      <button className="text-gray-400 hover:text-white" title="View"><Eye className="w-4 h-4" /></button>
                      <button className="text-gray-400 hover:text-white" title="Edit"><Edit className="w-4 h-4" /></button>
                      <button onClick={() => handleDelete(property.id)} className="text-gray-400 hover:text-red-400" title="Delete"><Trash2 className="w-4 h-4" /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {showAddImageModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/40" onClick={() => setShowAddImageModal(false)} />
          <div className="relative bg-neutral-900 border border-white/10 rounded-xl p-6 w-full max-w-md">
            <h2 className="text-xl font-semibold text-white mb-4">Add Image</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm text-gray-300 mb-1">Image URL</label>
                <input value={imageForm.url} onChange={e => setImageForm({ ...imageForm, url: e.target.value })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-sm text-gray-300 mb-1">Order</label>
                  <input type="number" min={1} value={imageForm.order} onChange={e => setImageForm({ ...imageForm, order: Number(e.target.value) })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
                </div>
                <div className="flex items-center gap-2 pt-6">
                  <input id="enabled" type="checkbox" checked={imageForm.enabled} onChange={e => setImageForm({ ...imageForm, enabled: e.target.checked })} />
                  <label htmlFor="enabled" className="text-sm text-gray-300">Enabled</label>
                </div>
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button onClick={() => setShowAddImageModal(false)} className="px-4 py-2 border border-white/20 rounded-lg text-gray-300 hover:bg-white/10">Cancel</button>
                <button onClick={handleAddImage} disabled={isSavingImage} className="px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100 disabled:opacity-50">{isSavingImage ? 'Saving...' : 'Add'}</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {showDeleteImageModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/40" onClick={() => setShowDeleteImageModal(false)} />
          <div className="relative bg-neutral-900 border border-white/10 rounded-xl p-6 w-full max-w-md">
            <h2 className="text-xl font-semibold text-white mb-4">Delete Image</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm text-gray-300 mb-1">Image ID</label>
                <input value={imageIdToDelete} onChange={e => setImageIdToDelete(e.target.value)} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button onClick={() => setShowDeleteImageModal(false)} className="px-4 py-2 border border-white/20 rounded-lg text-gray-300 hover:bg-white/10">Cancel</button>
                <button onClick={handleDeleteImage} disabled={isRemovingImage} className="px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100 disabled:opacity-50">{isRemovingImage ? 'Deleting...' : 'Delete'}</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default withAdminAuth(PropertiesPage);



