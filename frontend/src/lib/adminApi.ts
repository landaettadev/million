import { fetchWithAuth } from './auth/fetchWithAuth';

// Prefer NEXT_PUBLIC_API_BASE (host only), fallback to NEXT_PUBLIC_API_BASE_URL (may include /api)
const rawBase = (process.env.NEXT_PUBLIC_API_BASE || process.env.NEXT_PUBLIC_API_BASE_URL || '').replace(/\/$/, '');
const apiBase = rawBase.endsWith('/api') ? rawBase : `${rawBase}/api`;

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
  const url = new URL(`${apiBase}/admin/analytics/dashboard`);
  if (params?.startDate) url.searchParams.set('startDate', params.startDate);
  if (params?.endDate) url.searchParams.set('endDate', params.endDate);
  const res = await fetchWithAuth(url.toString());
  if (!res.ok) throw new Error('Failed to fetch dashboard analytics');
  return res.json() as Promise<DashboardAnalyticsDto>;
}

export async function getPropertyAnalytics(params?: { startDate?: string; endDate?: string; operationType?: 'sale' | 'rent' }) {
  const url = new URL(`${apiBase}/admin/analytics/properties`);
  if (params?.startDate) url.searchParams.set('startDate', params.startDate);
  if (params?.endDate) url.searchParams.set('endDate', params.endDate);
  if (params?.operationType) url.searchParams.set('operationType', params.operationType);
  const res = await fetchWithAuth(url.toString());
  if (!res.ok) throw new Error('Failed to fetch property analytics');
  return res.json() as Promise<PropertyAnalyticsDto>;
}

export async function getOwnerAnalytics(params?: { startDate?: string; endDate?: string }) {
  const url = new URL(`${apiBase}/admin/analytics/owners`);
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
  const res = await fetchWithAuth(`${apiBase}/admin/properties`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok) {
    let message = 'Failed to create property';
    try {
      const data = await res.json();
      message = data?.error || data?.title || message;
    } catch {
      try { message = await res.text(); } catch { /* ignore */ }
    }
    throw new Error(message);
  }
  return res.json() as Promise<{ id: string }>;
}

export async function deleteProperty(id: string) {
  const res = await fetchWithAuth(`${apiBase}/admin/properties/${id}`, { method: 'DELETE' });
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
  const res = await fetchWithAuth(`${apiBase}/admin/properties/${id}`, {
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
  const res = await fetchWithAuth(`${apiBase}/admin/owners`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok) throw new Error('Failed to create owner');
  return res.json() as Promise<{ id: string }>;
}

export async function deleteOwner(id: string) {
  const res = await fetchWithAuth(`${apiBase}/admin/owners/${id}`, { method: 'DELETE' });
  if (!res.ok && res.status !== 204) throw new Error('Failed to delete owner');
}

export type UpdateOwnerDto = CreateOwnerDto;

export async function updateOwner(id: string, dto: UpdateOwnerDto) {
  const res = await fetchWithAuth(`${apiBase}/admin/owners/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok && res.status !== 204) throw new Error('Failed to update owner');
}

// Owners - Read
export type AdminOwnerDto = {
  id: string;
  name: string;
  address: string;
  photo?: string | null;
  birthday?: string | null;
  propertiesCount: number;
  createdAt: string;
  updatedAt?: string | null;
  isDeleted: boolean;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export async function getOwners(params?: {
  searchTerm?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}) {
  const url = new URL(`${apiBase}/admin/owners`);
  if (params?.searchTerm) url.searchParams.set('searchTerm', params.searchTerm);
  if (params?.sortBy) url.searchParams.set('sortBy', params.sortBy);
  if (params?.sortDirection) url.searchParams.set('sortDirection', params.sortDirection);
  if (params?.page) url.searchParams.set('page', String(params.page));
  if (params?.pageSize) url.searchParams.set('pageSize', String(params.pageSize));
  const res = await fetchWithAuth(url.toString());
  if (!res.ok) throw new Error('Failed to fetch owners');
  return res.json() as Promise<PagedResult<AdminOwnerDto>>;
}

// Images
export type AddImageDto = {
  propertyId: string;
  file: string;
  enabled: boolean;
  order: number;
};

export async function addImage(dto: AddImageDto) {
  const res = await fetchWithAuth(`${apiBase}/admin/images`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(dto),
  });
  if (!res.ok) throw new Error('Failed to add image');
  return res.json() as Promise<{ id: string }>;
}

export async function deleteImage(id: string) {
  const res = await fetchWithAuth(`${apiBase}/admin/images/${id}`, { method: 'DELETE' });
  if (!res.ok && res.status !== 204) throw new Error('Failed to delete image');
}

export type PropertyImageDto = {
  id: string;
  propertyId: string;
  blobName: string;
  imageUrl: string;
  enabled: boolean;
  order: number;
  fileName: string;
  fileSize: number;
  contentType: string;
  createdAt: string;
};

export async function getPropertyImages(propertyId: string) {
  const res = await fetchWithAuth(`${apiBase}/admin/properties/${propertyId}/images`);
  if (!res.ok) throw new Error('Failed to fetch property images');
  return res.json() as Promise<PropertyImageDto[]>;
}

// Presigned upload (SAS)
export type PresignUploadRequest = {
  fileName: string;
  contentType: string;
  expiresSeconds?: number; // default 900
};

export type PresignUploadResponse = {
  blobName: string;
  uploadUrl: string;
  expiresAt: string;
  method: 'PUT';
};

export async function presignImageUpload(req: PresignUploadRequest) {
  const res = await fetchWithAuth(`${apiBase}/admin/images/presign`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
  if (!res.ok) throw new Error('Failed to presign upload');
  return res.json() as Promise<PresignUploadResponse>;
}

// Direct multipart upload to backend (saves to Azure Blob and DB)
export async function uploadPropertyImage(params: {
  propertyId: string;
  file: File;
  enabled?: boolean;
  order?: number;
}) {
  const form = new FormData();
  form.append('File', params.file);
  form.append('PropertyId', params.propertyId);
  form.append('Enabled', String(params.enabled ?? true));
  form.append('Order', String(params.order ?? 0));
  const res = await fetchWithAuth(`${apiBase}/admin/images/upload`, {
    method: 'POST',
    body: form,
  });
  if (!res.ok) throw new Error('Failed to upload image');
  return res.json() as Promise<{ imageId: string; blobName: string; imageUrl: string; fileName: string; fileSize: number; contentType: string }>;
}

// Admin properties read
export type AdminPropertyDto = {
  id: string;
  ownerId: string;
  ownerName: string;
  name: string;
  address: string;
  price: number;
  operationType: 'Sale' | 'Rent';
  description?: string | null;
  beds?: number | null;
  baths?: number | null;
  halfBaths?: number | null;
  sqft?: number | null;
  createdAt: string;
  updatedAt?: string | null;
  isDeleted: boolean;
};

export async function getAdminProperties(params?: {
  searchTerm?: string;
  ownerId?: string;
  minPrice?: number;
  maxPrice?: number;
  operationType?: 'Sale' | 'Rent';
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}) {
  const url = new URL(`${apiBase}/admin/properties`);
  if (params?.searchTerm) url.searchParams.set('searchTerm', params.searchTerm);
  if (params?.ownerId) url.searchParams.set('ownerId', params.ownerId);
  if (params?.minPrice != null) url.searchParams.set('minPrice', String(params.minPrice));
  if (params?.maxPrice != null) url.searchParams.set('maxPrice', String(params.maxPrice));
  if (params?.operationType) url.searchParams.set('operationType', params.operationType);
  if (params?.sortBy) url.searchParams.set('sortBy', params.sortBy);
  if (params?.sortDirection) url.searchParams.set('sortDirection', params.sortDirection);
  if (params?.page) url.searchParams.set('page', String(params.page));
  if (params?.pageSize) url.searchParams.set('pageSize', String(params.pageSize));
  const res = await fetchWithAuth(url.toString());
  if (!res.ok) throw new Error('Failed to fetch properties');
  return res.json() as Promise<PagedResult<AdminPropertyDto>>;
}

// Property status management
export async function markPropertySold(propertyId: string) {
  const res = await fetchWithAuth(`${apiBase}/admin/properties/${propertyId}/status`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status: 'sold' }),
  });
  if (!res.ok) throw new Error('Failed to mark property as sold');
  return res.json() as Promise<AdminPropertyDto>;
}

export async function markPropertyActive(propertyId: string) {
  const res = await fetchWithAuth(`${apiBase}/admin/properties/${propertyId}/status`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status: 'active' }),
  });
  if (!res.ok) throw new Error('Failed to mark property as active');
  return res.json() as Promise<AdminPropertyDto>;
}

// Videos
export async function setPropertyVideo(propertyId: string, url: string) {
  const res = await fetchWithAuth(`${apiBase}/admin/properties/${propertyId}/video`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ url }),
  });
  if (!res.ok) throw new Error('Failed to set property video');
  return res.json() as Promise<{ id: string; url: string }>;
}

export async function getPropertyVideo(propertyId: string) {
  const res = await fetchWithAuth(`${apiBase}/admin/properties/${propertyId}/video`);
  if (!res.ok) throw new Error('Failed to get property video');
  return res.json() as Promise<{ id: string; url: string | null }>;
}


