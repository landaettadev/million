import { fetchWithAuth } from './auth/fetchWithAuth';

const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || '';

// Properties
export type CreatePropertyDto = {
  ownerId: string;
  name: string;
  address: string;
  price: number;
  operationType: 'Sale' | 'Rent';
  beds?: number;
  baths?: number;
  halfBaths?: number;
  sqft?: number;
  description?: string;
};

export async function createProperty(dto: CreatePropertyDto) {
  const res = await fetchWithAuth(`${baseUrl}/api/admin/properties`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok) throw new Error('Failed to create property');
  return res.json() as Promise<{ id: string }>;
}

export async function deleteProperty(id: string) {
  const res = await fetchWithAuth(`${baseUrl}/api/admin/properties/${id}`, { method: 'DELETE' });
  if (!res.ok && res.status !== 204) throw new Error('Failed to delete property');
}

// Owners
export type CreateOwnerDto = {
  name: string;
  address: string;
  photo?: string | null;
  birthday?: string | null; // ISO
};

export async function createOwner(dto: CreateOwnerDto) {
  const res = await fetchWithAuth(`${baseUrl}/api/admin/owners`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok) throw new Error('Failed to create owner');
  return res.json() as Promise<{ id: string }>;
}

export async function deleteOwner(id: string) {
  const res = await fetchWithAuth(`${baseUrl}/api/admin/owners/${id}`, { method: 'DELETE' });
  if (!res.ok && res.status !== 204) throw new Error('Failed to delete owner');
}

// Images
export type AddImageDto = {
  propertyId: string;
  file: string;
  enabled: boolean;
  order: number;
};

export async function addImage(dto: AddImageDto) {
  const res = await fetchWithAuth(`${baseUrl}/api/admin/images`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok) throw new Error('Failed to add image');
  return res.json() as Promise<{ id: string }>;
}

export async function deleteImage(id: string) {
  const res = await fetchWithAuth(`${baseUrl}/api/admin/images/${id}`, { method: 'DELETE' });
  if (!res.ok && res.status !== 204) throw new Error('Failed to delete image');
}


