import { Home } from 'lucide-react'

export default function Footer() {
  return (
    <footer className="border-t border-slate-200 bg-white py-10">
      <div className="mx-auto max-w-7xl px-6">
        <div className="flex flex-col items-center justify-between gap-6 md:flex-row">
          <div className="flex items-center gap-3">
            <div
              className="flex h-8 w-8 items-center justify-center rounded-lg"
              style={{ backgroundColor: '#0d2137' }}
            >
              <Home className="h-4 w-4 text-blue-300" />
            </div>
            <div>
              <p className="text-sm font-bold leading-none text-slate-900">TaxSync</p>
              <p className="text-[10px] text-slate-500">
                Property Tax & Compliance Reporting System
              </p>
            </div>
          </div>

          <div className="flex items-center gap-6 text-xs text-slate-400">
            <a href="#features" className="transition-colors hover:text-slate-600">
              Features
            </a>
            <a href="#modules" className="transition-colors hover:text-slate-600">
              Modules
            </a>
            <a href="#roles" className="transition-colors hover:text-slate-600">
              Roles
            </a>
            <a href="#about" className="transition-colors hover:text-slate-600">
              Legal
            </a>
          </div>

          <p className="text-center text-xs text-slate-400">
            © 2026 Republic of the Philippines · Davao Region LGU Division
          </p>
        </div>
      </div>
    </footer>
  )
}
