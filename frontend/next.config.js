/** @type {import('next').NextConfig} */
const nextConfig = {
  images: {
    domains: ['picsum.photos', 'images.unsplash.com'],
  },
  experimental: {
    typedRoutes: true,
  },
  // Avoid OneDrive locking default .next folder by using a custom distDir
  distDir: '.next-dev',
  async redirects() {
    return [];
  },
}

module.exports = nextConfig
