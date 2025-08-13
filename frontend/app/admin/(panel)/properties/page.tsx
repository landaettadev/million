'use client';

import { withAdminAuth } from '../../../../src/lib/auth/AdminAuthContext';
import { Plus, Search, Filter, Eye, Edit, Trash2, Image as ImageIcon, CheckCircle2, RotateCcw, Video } from 'lucide-react';
import { ImageManagerModal } from '../../../../components/admin/ImageManagerModal';
import { createProperty, deleteProperty, updateProperty, getAdminProperties, type AdminPropertyDto, getOwners, type AdminOwnerDto, uploadPropertyImage, deleteImage, getPropertyImages, type PropertyImageDto, markPropertySold, markPropertyActive, setPropertyVideo, getPropertyVideo, setFeatured } from '../../../../src/lib/adminApi';
import { useEffect, useState } from 'react';

function PropertiesPage() {
  const [isCreating, setIsCreating] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState({ name: '', address: '', price: 0, operationType: 'Sale' as 'Sale' | 'Rent', beds: 0, baths: 0, halfBaths: 0, sqft: 0, description: '', status: 'Active' as 'Active' | 'Sold' });
  const [showAddImageModal, setShowAddImageModal] = useState(false);
  const [showDeleteImageModal, setShowDeleteImageModal] = useState(false);
  const [showImageManagerModal, setShowImageManagerModal] = useState(false);
  const [activePropertyId, setActivePropertyId] = useState<string | null>(null);
  const [activePropertyName, setActivePropertyName] = useState<string>('');
  // Video assignment modal state
  const [showVideoModal, setShowVideoModal] = useState(false);
  const [videoPropertyId, setVideoPropertyId] = useState<string | null>(null);
  const [videoPropertyName, setVideoPropertyName] = useState<string>('');
  const [videoUrl, setVideoUrl] = useState<string>('');
  const [imageForm, setImageForm] = useState<{ file: File | null; order: number; enabled: boolean }>({ file: null, order: 1, enabled: true });
  const [imageIdToDelete, setImageIdToDelete] = useState('');
  const [isSavingImage, setIsSavingImage] = useState(false);
  const [isRemovingImage, setIsRemovingImage] = useState(false);
  const [properties, setProperties] = useState<AdminPropertyDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [owners, setOwners] = useState<AdminOwnerDto[]>([]);
  const [selectedOwnerId, setSelectedOwnerId] = useState<string | null>(null);
  const [propertyImages, setPropertyImages] = useState<Record<string, string[]>>({});
  
  // Search and filter states
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [filteredProperties, setFilteredProperties] = useState<AdminPropertyDto[]>([]);

  // Debounce search term
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(searchTerm);
    }, 300);

    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Filter and search functionality
  useEffect(() => {
    let filtered = [...properties];

    // Apply search filter
    if (debouncedSearchTerm.trim()) {
      const searchLower = debouncedSearchTerm.toLowerCase();
      filtered = filtered.filter(property => 
        property.name.toLowerCase().includes(searchLower) ||
        property.address.toLowerCase().includes(searchLower) ||
        (property.description && property.description.toLowerCase().includes(searchLower))
      );
    }

    // Apply status filter
    if (statusFilter !== 'all') {
      filtered = filtered.filter(property => {
        switch (statusFilter) {
          case 'active':
            return !property.isDeleted;
          case 'sold':
            return property.isDeleted;
          case 'pending':
            // For now, we'll consider properties without images as pending
            return !propertyImages[property.id] || propertyImages[property.id].length === 0;
          default:
            return true;
        }
      });
    }

    setFilteredProperties(filtered);
  }, [properties, debouncedSearchTerm, statusFilter, propertyImages]);

  // Load properties and owners function
  const load = async () => {
    setLoading(true);
    try {
      const [propsRes, ownersRes] = await Promise.all([
        getAdminProperties({ page: 1, pageSize: 50 }),
        getOwners({ page: 1, pageSize: 50 }),
      ]);
      setProperties(propsRes.items);
      setOwners(ownersRes.items);
      if (!selectedOwnerId && ownersRes.items.length > 0) {
        setSelectedOwnerId(ownersRes.items[0].id);
      }
      // Load images for each property
      const imagePromises = propsRes.items.map(async (property) => {
        try {
          const images = await getPropertyImages(property.id);
          return { propertyId: property.id, images: images.map(img => img.imageUrl) };
        } catch {
          return { propertyId: property.id, images: [] };
        }
      });
      const imageResults = await Promise.all(imagePromises);
      const imagesMap: Record<string, string[]> = {};
      imageResults.forEach(({ propertyId, images }) => {
        imagesMap[propertyId] = images;
      });
      setPropertyImages(imagesMap);
    } finally {
      setLoading(false);
    }
  };

  // Load properties and owners on mount
  useEffect(() => {
    load();
  }, []);

  const handleNew = async () => {
    if (!selectedOwnerId) {
      alert('Please create/select an owner first');
      return;
    }
    try {
      setIsCreating(true);
      await createProperty({
        ownerId: selectedOwnerId,
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
      const res = await getAdminProperties({ page: 1, pageSize: 50 });
      setProperties(res.items);
    } finally {
      setIsCreating(false);
    }
  };

  const handleDelete = async (id: string) => {
    await deleteProperty(id);
    const res = await getAdminProperties({ page: 1, pageSize: 50 });
    setProperties(res.items);
  };

  const openEdit = (p: any) => {
    setEditId(p.id);
    setEditForm({
      name: p.name,
      address: p.address,
      price: p.price || 100000,
      operationType: p.operationType || 'Sale',
      beds: Number(p.beds || 0),
      baths: Number(p.baths || 0),
      halfBaths: Number(p.halfBaths || 0),
      sqft: Number(p.sqft || 0),
      description: p.description || '',
      status: p.isDeleted ? 'Sold' : 'Active',
    });
    setShowEditModal(true);
  };

  const handleSaveEdit = async () => {
    if (!editId) return;
    // If status changed, call sold/active endpoints
    if (editForm.status === 'Sold') {
      await markPropertySold(editId);
    } else {
      await markPropertyActive(editId);
    }
    // Update other fields
    await updateProperty(editId, editForm);
    const res = await getAdminProperties({ page: 1, pageSize: 50 });
    setProperties(res.items);
    setShowEditModal(false);
    setEditId(null);
  };

  const toggleSold = async (p: AdminPropertyDto) => {
    if (p.isDeleted) {
      await markPropertyActive(p.id);
    } else {
      await markPropertySold(p.id);
    }
    const res = await getAdminProperties({ page: 1, pageSize: 50 });
    setProperties(res.items);
  };

  const openAddImage = (propertyId: string) => {
    setActivePropertyId(propertyId);
    setImageForm({ file: null, order: 1, enabled: true });
    setShowAddImageModal(true);
  };

  const openImageManager = (propertyId: string, propertyName: string) => {
    setActivePropertyId(propertyId);
    setActivePropertyName(propertyName);
    setShowImageManagerModal(true);
  };

  const openAssignVideo = async (propertyId: string, propertyName: string) => {
    setVideoPropertyId(propertyId);
    setVideoPropertyName(propertyName);
    try {
      // Prefer backend value if present
      const apiRes = await getPropertyVideo(propertyId).catch(() => null);
      const backendUrl = apiRes?.url || '';
      let localUrl = '';
      try {
        const raw = typeof window !== 'undefined' ? localStorage.getItem('propertyVideoMap') : null;
        if (raw) {
          const map = JSON.parse(raw) as Record<string, string>;
          localUrl = map[propertyId] || '';
        }
      } catch { /* ignore */ }
      setVideoUrl(backendUrl || localUrl || '');
    } catch {
      setVideoUrl('');
    }
    setShowVideoModal(true);
  };

  const saveAssignedVideo = async () => {
    if (!videoPropertyId) return;
    try {
      // Persist in backend
      const trimmed = (videoUrl || '').trim();
      await setPropertyVideo(videoPropertyId, trimmed || '');
      const raw = typeof window !== 'undefined' ? localStorage.getItem('propertyVideoMap') : null;
      const map: Record<string, string> = raw ? JSON.parse(raw) : {};
      if (trimmed) {
        map[videoPropertyId] = trimmed;
      } else {
        delete map[videoPropertyId];
      }
      localStorage.setItem('propertyVideoMap', JSON.stringify(map));
      // Notify other tabs/components to refresh hoverVideo immediately
      try {
        window.dispatchEvent(new CustomEvent('propertyVideoMap:changed', { detail: { propertyId: videoPropertyId, url: trimmed } }));
      } catch { /* ignore */ }
      setShowVideoModal(false);
    } catch (e) {
      console.error('Failed to save video mapping', e);
      alert('Failed to save video mapping');
    }
  };

  const handleAddImage = async () => {
    if (!activePropertyId || !imageForm.file) return;
    setIsSavingImage(true);
    try {
      await uploadPropertyImage({
        propertyId: activePropertyId,
        file: imageForm.file,
        enabled: imageForm.enabled,
        order: imageForm.order,
      });
      // Reload images for this property
      try {
        const images = await getPropertyImages(activePropertyId);
        setPropertyImages(prev => ({
          ...prev,
          [activePropertyId]: images.map(img => img.imageUrl)
        }));
      } catch (error) {
        console.error('Failed to reload property images:', error);
      }
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
          <p className="text-gray-400 mt-2">
            {loading ? 'Loading properties...' : `Showing ${filteredProperties.length} of ${properties.length} properties`}
          </p>
        </div>
        <div className="flex items-center gap-3 mt-4 sm:mt-0">
          <select value={selectedOwnerId ?? ''} onChange={e => setSelectedOwnerId(e.target.value || null)} className="px-3 py-2 bg-neutral-900 border border-white/10 rounded-lg text-white">
            <option value="">Select owner</option>
            {owners.map(o => (
              <option key={o.id} value={o.id}>{o.name}</option>
            ))}
          </select>
          <button onClick={handleNew} disabled={isCreating} className="inline-flex items-center px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100 font-medium transition-colors disabled:opacity-50">
          <Plus className="w-4 h-4 mr-2" />
          {isCreating ? 'Creating...' : 'New Property'}
          </button>
        </div>
      </div>

      <div className="flex flex-col sm:flex-row gap-4">
        <div className="relative flex-1">
          <Search className={`absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 ${
            searchTerm !== debouncedSearchTerm ? 'text-blue-400 animate-pulse' : 'text-gray-400'
          }`} />
          <input 
            type="text" 
            placeholder="Search properties..." 
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 bg-neutral-900 border border-white/10 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-white/20" 
          />
          {searchTerm && (
            <button
              onClick={() => setSearchTerm('')}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-white"
              title="Clear search"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>
        <div className="relative">
          <Filter className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
          <select 
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="pl-10 pr-8 py-2 bg-neutral-900 border border-white/10 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-white/20 appearance-none"
          >
            <option value="all">All Status</option>
            <option value="active">Active</option>
            <option value="sold">Sold</option>
            <option value="pending">Pending</option>
          </select>
        </div>
        {(searchTerm || statusFilter !== 'all') && (
          <button
            onClick={() => {
              setSearchTerm('');
              setStatusFilter('all');
            }}
            className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors whitespace-nowrap"
          >
            Clear Filters
          </button>
        )}
      </div>

      <div className="bg-neutral-900 border border-white/10 rounded-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-white/5">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Property</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Images</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Price</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Details</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Owner</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Featured</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/10">
              {loading ? (
                <tr><td className="px-6 py-4 text-gray-400" colSpan={7}>Loading properties…</td></tr>
              ) : filteredProperties.length === 0 ? (
                <tr><td className="px-6 py-4 text-gray-400 text-center" colSpan={7}>
                  {searchTerm || statusFilter !== 'all' ? 'No properties match your search criteria' : 'No properties found'}
                </td></tr>
              ) : filteredProperties.map((property) => (
                <tr key={property.id} className="hover:bg-white/5">
                  <td className="px-6 py-4">
                    <div>
                      <div className="text-sm font-medium text-white">{property.name}</div>
                      <div className="text-sm text-gray-400">{property.address}</div>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex space-x-1">
                      {propertyImages[property.id]?.slice(0, 3).map((imageUrl, index) => (
                        <img
                          key={index}
                          src={imageUrl}
                          alt={`Property ${property.name} image ${index + 1}`}
                          className="w-10 h-10 object-cover rounded border border-white/10"
                          onError={e => {
                            const target = e.target as HTMLImageElement;
                            target.style.display = 'none';
                          }}
                        />
                      )) || (
                        <span className="text-xs text-gray-500">No images</span>
                      )}
                      {propertyImages[property.id] && propertyImages[property.id].length > 3 && (
                        <div className="w-10 h-10 bg-gray-700 rounded border border-white/10 flex items-center justify-center">
                          <span className="text-xs text-gray-300">+{propertyImages[property.id].length - 3}</span>
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-white">${property.price?.toLocaleString() || 'Price on request'}</div>
                    <div className="text-sm text-gray-400">{property.operationType}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-300">{property.beds ?? 0} bed, {property.baths ?? 0} bath</div>
                    <div className="text-sm text-gray-400">{property.sqft ?? 0} sq ft</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-300">{property.ownerName}</td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      property.isDeleted 
                        ? 'bg-red-100 text-red-800' 
                        : 'bg-green-100 text-green-800'
                    }`}>
                      {property.isDeleted ? 'Sold' : 'Active'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-400">
                    <div className="flex items-center gap-2">
                      <button onClick={() => openAddImage(property.id)} className="text-gray-400 hover:text-white" title="Add image"><Plus className="w-4 h-4" /></button>
                      <button onClick={() => openImageManager(property.id, property.name)} className="text-gray-400 hover:text-white" title="Manage images"><ImageIcon className="w-4 h-4" /></button>
                      <button onClick={() => openAssignVideo(property.id, property.name)} className="text-gray-400 hover:text-white" title="Assign video"><Video className="w-4 h-4" /></button>
                      <button onClick={() => openDeleteImage()} className="text-gray-400 hover:text-white" title="Delete image by ID"><Trash2 className="w-4 h-4" /></button>
                      <button className="text-gray-400 hover:text-white" title="View"><Eye className="w-4 h-4" /></button>
                      <button onClick={() => openEdit(property)} className="text-gray-400 hover:text-white" title="Edit"><Edit className="w-4 h-4" /></button>
                      <button onClick={() => toggleSold(property)} className={`text-gray-400 hover:text-white`} title={property.isDeleted ? 'Mark Active' : 'Mark Sold'}>
                        {property.isDeleted ? <RotateCcw className="w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />}
                      </button>
                      <button onClick={() => handleDelete(property.id)} className="text-gray-400 hover:text-red-400" title="Delete"><Trash2 className="w-4 h-4" /></button>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <label className="inline-flex items-center gap-2 text-sm text-gray-300">
                      <input
                        type="checkbox"
                        defaultChecked={property.isFeatured as any}
                        onChange={async (e) => {
                          try {
                            await setFeatured(property.id, e.target.checked)
                          } catch {
                            e.currentTarget.checked = !e.currentTarget.checked
                            alert('Failed to update featured flag')
                          }
                        }}
                      />
                      Featured
                    </label>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {showEditModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/40" onClick={() => setShowEditModal(false)} />
          <div className="relative bg-neutral-900 border border-white/10 rounded-xl p-6 w-full max-w-lg">
            <h2 className="text-xl font-semibold text-white mb-4">Edit Property</h2>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm text-gray-300 mb-1">Name</label>
                <input value={editForm.name} onChange={e => setEditForm({ ...editForm, name: e.target.value })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Address</label>
                <input value={editForm.address} onChange={e => setEditForm({ ...editForm, address: e.target.value })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Price</label>
                <input type="number" value={editForm.price} onChange={e => setEditForm({ ...editForm, price: Number(e.target.value) })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Operation</label>
                <select value={editForm.operationType} onChange={e => setEditForm({ ...editForm, operationType: e.target.value as any })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white">
                  <option value="Sale">Sale</option>
                  <option value="Rent">Rent</option>
                </select>
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Status</label>
                <select value={editForm.status} onChange={e => setEditForm({ ...editForm, status: e.target.value as any })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white">
                  <option value="Active">Active</option>
                  <option value="Sold">Sold</option>
                </select>
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Beds</label>
                <input type="number" value={editForm.beds} onChange={e => setEditForm({ ...editForm, beds: Number(e.target.value) })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Baths</label>
                <input type="number" value={editForm.baths} onChange={e => setEditForm({ ...editForm, baths: Number(e.target.value) })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Half Baths</label>
                <input type="number" value={editForm.halfBaths} onChange={e => setEditForm({ ...editForm, halfBaths: Number(e.target.value) })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Sqft</label>
                <input type="number" value={editForm.sqft} onChange={e => setEditForm({ ...editForm, sqft: Number(e.target.value) })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div className="col-span-2">
                <label className="block text-sm text-gray-300 mb-1">Description</label>
                <textarea value={editForm.description} onChange={e => setEditForm({ ...editForm, description: e.target.value })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
            </div>
            <div className="flex justify-end gap-2 pt-4">
              <button onClick={() => setShowEditModal(false)} className="px-4 py-2 border border-white/20 rounded-lg text-gray-300 hover:bg-white/10">Cancel</button>
              <button onClick={handleSaveEdit} className="px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100">Save</button>
            </div>
          </div>
        </div>
      )}

      {showAddImageModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/40" onClick={() => setShowAddImageModal(false)} />
          <div className="relative bg-neutral-900 border border-white/10 rounded-xl p-6 w-full max-w-md">
            <h2 className="text-xl font-semibold text-white mb-4">Add Image</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm text-gray-300 mb-1">Select Image File</label>
                <input 
                  type="file" 
                  accept="image/*" 
                  onChange={e => setImageForm({ ...imageForm, file: e.target.files?.[0] || null })} 
                  className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white file:mr-4 file:py-1 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-white file:text-black hover:file:bg-gray-100" 
                />
                {imageForm.file && (
                  <p className="text-sm text-gray-400 mt-1">Selected: {imageForm.file.name}</p>
                )}
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

      {/* Image Manager Modal */}
      <ImageManagerModal
        isOpen={showImageManagerModal}
        onClose={() => setShowImageManagerModal(false)}
        propertyId={activePropertyId || ''}
        propertyName={activePropertyName}
        onImageDeleted={() => {
          // Refresh all properties after image deletion
          load();
        }}
      />

      {/* Assign Video Modal */}
      {showVideoModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/40" onClick={() => setShowVideoModal(false)} />
          <div className="relative bg-neutral-900 border border-white/10 rounded-xl p-6 w-full max-w-lg">
            <h2 className="text-xl font-semibold text-white mb-4">Assign video</h2>
            <p className="text-sm text-gray-400 mb-3">Property: <span className="text-white">{videoPropertyName}</span></p>
            <label className="block text-sm text-gray-300 mb-1">Video URL (mp4)</label>
            <input
              value={videoUrl}
              onChange={(e) => setVideoUrl(e.target.value)}
              placeholder="http://localhost:5244/videos/lujosa1.mp4"
              className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white mb-4"
            />
            <div className="flex justify-between text-xs text-gray-400 mb-4">
              <span>Leave empty to remove assignment</span>
              <span>Local files: /videos/...</span>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <button onClick={() => setShowVideoModal(false)} className="px-4 py-2 border border-white/20 rounded-lg text-gray-300 hover:bg-white/10">Cancel</button>
              <button onClick={saveAssignedVideo} className="px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100">Save</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default withAdminAuth(PropertiesPage);



