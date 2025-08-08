import Link from 'next/link'
import Image from 'next/image'
import { Skeleton } from '@/components/ui/Skeleton'
import { formatPrice } from '@/lib/format'
import type { PropertyLiteDto } from '@/lib/types'

type CardItem = PropertyLiteDto & {
  halfBaths?: number
  projectName?: string
}

interface PropertyCardProps {
  item: CardItem
  loading?: boolean
}

export function PropertyCard({ item, loading = false }: PropertyCardProps) {
  if (loading) {
    return (
      <article className="relative group transition-all duration-200">
        <div className="w-full h-[360px] md:h-[560px] bg-gray-200 rounded-3xl shadow-xl" />
        <div className="absolute left-6 right-6 sm:right-auto bottom-6 max-w-[560px] rounded-2xl bg-white text-black shadow-xl ring-1 ring-black/5 p-6 md:p-7">
          <Skeleton className="h-4 w-24 bg-gray-200 mb-2" aria-label="Loading project name" />
          <Skeleton className="h-7 w-3/4 bg-gray-200 mb-3" aria-label="Loading title" />
          <Skeleton className="h-4 w-2/3 bg-gray-200 mb-4" aria-label="Loading specs" />
          <Skeleton className="h-6 w-1/3 bg-gray-200" aria-label="Loading price" />
        </div>
      </article>
    )
  }

  const specs: string[] = []
  if (item.beds) specs.push(`${item.beds} Beds`)
  if (item.baths) specs.push(`${item.baths} Baths`)
  if (typeof item.halfBaths === 'number') {
    specs.push(`${item.halfBaths} ${item.halfBaths === 1 ? 'Half Bath' : 'Half Baths'}`)
  }
  if (item.sqft) specs.push(`${item.sqft.toLocaleString()} Sq. Ft.`)

  return (
    <Link href={`/properties/${item.id}`} aria-label={`View details of ${item.name}`} className="block">
      <article className="relative group transition-all duration-200 transform-gpu hover:-translate-y-0.5">
        {/* Big image */}
        <div className="relative w-full h-[360px] md:h-[560px] rounded-3xl overflow-hidden shadow-xl group-hover:shadow-2xl">
          {item.image ? (
            <Image
              src={item.image}
              alt={`${item.name} — ${item.address}`}
              fill
              className="w-full h-full object-cover transition-transform duration-300 ease-out group-hover:scale-[1.01]"
              sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 50vw"
            />
          ) : (
            <div className="w-full h-full bg-gray-200" />
          )}
        </div>

        {/* Floating info panel */}
        <div className="absolute left-6 right-6 sm:right-auto bottom-6 max-w-[560px] rounded-2xl bg-white text-black shadow-xl ring-1 ring-black/5 p-6 md:p-7 transition-transform duration-200 transform-gpu group-hover:-translate-y-[2px]">
          {item.projectName && (
            <div className="text-[13px] text-gray-500 mb-1">{item.projectName}</div>
          )}
          <h3 className="font-serif text-xl md:text-2xl leading-snug">
            {item.name}
          </h3>
          {specs.length > 0 && (
            <div className="mt-3 text-gray-600 text-sm md:text-[15px] flex flex-wrap gap-x-6 gap-y-1">
              {specs.map((s, i) => (
                <span key={i} className="tracking-wide uppercase">{s}</span>
              ))}
            </div>
          )}
          <div className="mt-5 text-xl md:text-2xl font-semibold tracking-wide">
            {formatPrice(item.price)}
          </div>
        </div>
      </article>
    </Link>
  )
}
