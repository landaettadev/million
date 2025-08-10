import { fetchWithAuth } from './auth/fetchWithAuth';

const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || '';

// Analytics
export type DashboardAnalyticsDto = {
  totalProperties: number;
  totalOwners: number;
  activeProperties: number;
  pendingProperties: number;
  totalRevenue: number;
  monthlyRevenue: number;
  yearlyRevenue: number;
  propertiesByMonth: { label: string; value: number; count: number }[];
  revenueByMonth: { label: string; value: number; count: number }[];
  propertiesByOperationType: { label: string; value: number; count: number }[];
};

export type PropertyAnalyticsDto = {
  totalProperties: number;
  saleProperties: number;
  rentProperties: number;
  averagePrice: number;
  averageRentPrice: number;
  averageSalePrice: number;
  propertiesByLocation: { label: string; value: number; count: number }[];
  propertiesByPriceRange: { label: string; value: number; count: number }[];
  propertiesByBedrooms: { label: string; value: number; count: number }[];
  propertiesByBathrooms: { label: string; value: number; count: number }[];
};

export type OwnerAnalyticsDto = {
  totalOwners: number;
  activeOwners: number;
  newOwnersThisMonth: number;
  averagePropertiesPerOwner: number;
  ownersByMonth: { label: string; value: number; count: number }[];
  topOwnersByProperties: { label: string; value: number; count: number }[];
};

export type RevenueAnalyticsDto = {
  totalRevenue: number;
  averageRevenue: number;
  revenueGrowth: number;
  revenueByPeriod: { label: string; value: number; count: number }[];
  revenueByOperationType: { label: string; value: number; count: number }[];
  revenueByLocation: { label: string; value: number; count: number }[];
};

export async function getDashboardAnalytics(params?: { startDate?: string; endDate?: string }) {
  const url = new URL(`${baseUrl}/api/admin/analytics/dashboard`);
  if (params?.startDate) url.searchParams.set('startDate', params.startDate);
  if (params?.endDate) url.searchParams.set('endDate', params.endDate);
  const res = await fetchWithAuth(url.toString());
  if (!res.ok) throw new Error('Failed to fetch dashboard analytics');
  return res.json() as Promise<DashboardAnalyticsDto>;
}

export async function getPropertyAnalytics(params?: { startDate?: string; endDate?: string; operationType?: 'sale' | 'rent' }) {
  const url = new URL(`${baseUrl}/api/admin/analytics/properties`);
  if (params?.startDate) url.searchParams.set('startDate', params.startDate);
  if (params?.endDate) url.searchParams.set('endDate', params.endDate);
  if (params?.operationType) url.searchParams.set('operationType', params.operationType);
  const res = await fetchWithAuth(url.toString());
  if (!res.ok) throw new Error('Failed to fetch property analytics');
  return res.json() as Promise<PropertyAnalyticsDto>;
}

export async function getOwnerAnalytics(params?: { startDate?: string; endDate?: string }) {
  const url = new URL(`${baseUrl}/api/admin/analytics/owners`);
  if (params?.startDate) url.searchParams.set('startDate', params.startDate);
  if (params?.endDate) url.searchParams.set('endDate', params.endDate);
  const res = await fetchWithAuth(url.toString());
  if (!res.ok) throw new Error('Failed to fetch owner analytics');
  return res.json() as Promise<OwnerAnalyticsDto>;
}

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

export type UpdatePropertyDto = {
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

export async function updateProperty(id: string, dto: UpdatePropertyDto) {
  const res = await fetchWithAuth(`${baseUrl}/api/admin/properties/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok && res.status !== 204) throw new Error('Failed to update property');
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

export type UpdateOwnerDto = CreateOwnerDto;

export async function updateOwner(id: string, dto: UpdateOwnerDto) {
  const res = await fetchWithAuth(`${baseUrl}/api/admin/owners/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok && res.status !== 204) throw new Error('Failed to update owner');
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


