'use client';

import { withAdminAuth } from '../../../../src/lib/auth/AdminAuthContext';
import { createOwner, deleteOwner, updateOwner } from '../../../../src/lib/adminApi';
import { Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';

function OwnersAdminPage() {
  const [owners, setOwners] = useState<Array<{ id: string; name: string; email?: string; phone?: string; properties?: number; totalValue?: string }>>([
    { id: '1', name: 'John Doe', properties: 3, totalValue: '$8,200,000' },
    { id: '2', name: 'Jane Smith', properties: 2, totalValue: '$5,800,000' },
  ]);
  const [isCreating, setIsCreating] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState({ name: '', address: '' });
  const [editId, setEditId] = useState<string | null>(null);

  const handleCreate = async () => {
    if (!form.name || !form.address) return;
    try {
      setIsCreating(true);
      const res = await createOwner({ name: form.name, address: form.address });
      setOwners([{ id: res.id, name: form.name, properties: 0, totalValue: '$0' }, ...owners]);
      setShowModal(false);
      setForm({ name: '', address: '' });
    } finally {
      setIsCreating(false);
    }
  };

  const handleDelete = async (id: string) => {
    await deleteOwner(id);
    setOwners(owners.filter(o => o.id !== id));
  };

  const openEdit = (owner: { id: string; name: string }) => {
    setEditId(owner.id);
    setForm({ name: owner.name, address: '' });
    setShowModal(true);
  };

  const handleSave = async () => {
    if (!form.name || !form.address) return;
    if (!editId) {
      await handleCreate();
      return;
    }
    setIsCreating(true);
    try {
      await updateOwner(editId, { name: form.name, address: form.address });
      setOwners(owners.map(o => (o.id === editId ? { ...o, name: form.name } : o)));
      setShowModal(false);
      setEditId(null);
      setForm({ name: '', address: '' });
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold text-white">Owners</h1>
        <button onClick={() => setShowModal(true)} className="inline-flex items-center px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100 font-medium transition-colors">
          <Plus className="w-4 h-4 mr-2" /> New Owner
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
        {owners.map((owner) => (
          <div key={owner.id} className="bg-neutral-900 border border-white/10 rounded-xl p-6">
            <div className="flex items-center justify-between mb-2">
              <h3 className="text-lg font-semibold text-white">{owner.name}</h3>
              <button onClick={() => handleDelete(owner.id)} className="text-gray-400 hover:text-red-400" title="Delete">
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
            <div className="pt-2">
              <button onClick={() => openEdit(owner)} className="text-xs text-gray-300 underline">Edit</button>
            </div>
            <div className="text-sm text-gray-400">Properties: {owner.properties ?? 0}</div>
            <div className="text-sm text-gray-400">Total Value: {owner.totalValue ?? '-'}</div>
          </div>
        ))}
      </div>

      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/40" onClick={() => setShowModal(false)} />
          <div className="relative bg-neutral-900 border border-white/10 rounded-xl p-6 w-full max-w-md">
            <h2 className="text-xl font-semibold text-white mb-4">{editId ? 'Edit Owner' : 'Create Owner'}</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm text-gray-300 mb-1">Name</label>
                <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div>
                <label className="block text-sm text-gray-300 mb-1">Address</label>
                <input value={form.address} onChange={e => setForm({ ...form, address: e.target.value })} className="w-full px-3 py-2 bg-black border border-white/20 rounded-lg text-white" />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button onClick={() => { setShowModal(false); setEditId(null); }} className="px-4 py-2 border border-white/20 rounded-lg text-gray-300 hover:bg-white/10">Cancel</button>
                <button onClick={handleSave} disabled={isCreating} className="px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-100 disabled:opacity-50">{isCreating ? 'Saving...' : editId ? 'Save' : 'Create'}</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default withAdminAuth(OwnersAdminPage);


