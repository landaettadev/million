/** @type {import('next').NextConfig} */
const nextConfig = {
  images: {
    domains: [
      'picsum.photos', 
      'images.unsplash.com',
      'millionstorageprod.blob.core.windows.net',
      'localhost'
    ],
  },
  experimental: {
    typedRoutes: true,
  },
  env: {
    // Fallback for local development if .env.local is missing
    NEXT_PUBLIC_API_BASE: process.env.NEXT_PUBLIC_API_BASE || 'http://localhost:5244',
    NEXT_PUBLIC_API_BASE_URL: process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5244/api',
  },
  // Avoid OneDrive locking default .next folder by using a custom distDir
  distDir: '.next-dev',
  async redirects() {
    return [];
  },
}

module.exports = nextConfig
