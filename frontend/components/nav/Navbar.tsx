import Link from 'next/link'
import { Button } from '@/components/ui/Button'

export function Navbar() {
  return (
    <nav className="h-16 flex items-center justify-between">
      {/* Left: Logo */}
      <div className="flex items-center gap-8">
        <Link href="/" className="font-serif text-2xl tracking-tight smooth hover:opacity-80">
          MILLION
        </Link>

        <ul className="hidden md:flex items-center gap-6 text-sm text-muted">
          <li>
            <Link href="/properties" className="smooth hover:text-white">Properties</Link>
          </li>
          <li>
            <a className="cursor-not-allowed opacity-60" title="Mock">New Developments</a>
          </li>
          <li>
            <a className="cursor-not-allowed opacity-60" title="Mock">Transactions</a>
          </li>
          <li>
            <a className="cursor-not-allowed opacity-60" title="Mock">Team</a>
          </li>
        </ul>
      </div>

      {/* Right: CTA */}
      <div>
        <Button
          variant="outline"
          className="border-white/30 text-white hover:bg-white hover:text-black smooth"
        >
          Sell your unit
        </Button>
      </div>
    </nav>
  )
}
