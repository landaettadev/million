export function Footer() {
  return (
    <div className="py-8 flex flex-col md:flex-row items-start md:items-center justify-between gap-4 text-sm text-muted">
      <p>
        © {new Date().getFullYear()} MILLION. All rights reserved.
      </p>
      <p className="opacity-80">
        Built with <span className="text-white">Next.js</span> + <span className="text-white">Tailwind</span> (mock)
      </p>
    </div>
  )
}
