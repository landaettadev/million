import type { Metadata } from 'next'
import './globals.css'
import { Navbar } from '@/components/nav/Navbar'
import { Footer } from '@/components/footer/Footer'

export const metadata: Metadata = {
  title: 'MILLION Luxury - Premium Real Estate',
  description: 'Discover exclusive luxury properties with MILLION. Premium real estate listings with exceptional service.',
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className="scroll-smooth">
      <body className="min-h-screen bg-black text-white">
        <header className="sticky top-0 z-50 surface border-b border-subtle">
          <div className="container">
            <Navbar />
          </div>
        </header>

        <main className="flex-1">
          {children}
        </main>

        <footer className="border-t border-subtle mt-16">
          <div className="container">
            <Footer />
          </div>
        </footer>
      </body>
    </html>
  )
}
