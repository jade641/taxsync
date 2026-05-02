import { useState, useMemo } from "react";
import { Plus, Search, Filter, Download, Edit3, Trash2, Home, X, AlertCircle, CheckCircle, Lock, AlertTriangle, Eye, MapPin } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { ReadOnlyBanner, LimitedAccessBanner } from "../components/RoleGuard";

// ─── Types ────────────────────────────────────────────────────────────────────
type PropertyType = "Residential" | "Commercial" | "Agricultural" | "Industrial" | "Special";
type PropertyStatus = "Registered" | "Pending Review" | "Delinquent";

type Property = {
  id: string;
  pin: string;           // Property Identification Number
  ownerName: string;
  barangay: string;
  propertyType: PropertyType;
  lotNumber: string;
  areaSqm: number;
  marketValue: number;
  assessmentLevel: number; // percentage e.g. 20 for 20%
  assessedValue: number;
  status: PropertyStatus;
  dateRegistered: string;
  anomaly?: string;      // ML anomaly flag
};

// ─── Mock Data ────────────────────────────────────────────────────────────────
const INITIAL_PROPERTIES: Property[] = [];

const BARANGAYS = ["All", "Brgy. Sta. Ana", "Brgy. San Jose", "Brgy. Poblacion", "Brgy. Bagong Silang", "Brgy. Tibagan", "Brgy. San Isidro", "Brgy. Sto. Niño", "Brgy. Mabini"];
const PROPERTY_TYPES: PropertyType[] = ["Residential", "Commercial", "Agricultural", "Industrial", "Special"];
const ASSESSMENT_LEVELS: Record<PropertyType, number> = {
  Residential: 20, Commercial: 50, Agricultural: 40, Industrial: 80, Special: 0,
};

type ModalMode = "add" | "edit" | "delete" | "view" | null;
const EMPTY_FORM = {
  ownerName: "", barangay: "Brgy. Poblacion", propertyType: "Residential" as PropertyType,
  lotNumber: "", areaSqm: "", marketValue: "", status: "Registered" as PropertyStatus,
};

const fmt = (val: number) => `₱ ${val.toLocaleString("en-PH", { minimumFractionDigits: 2 })}`;
const fmtShort = (val: number) => val >= 1000000 ? `₱${(val / 1000000).toFixed(2)}M` : `₱${(val / 1000).toFixed(0)}K`;

const STATUS_CFG: Record<PropertyStatus, { cls: string; dot: string }> = {
  "Registered":    { cls: "bg-emerald-100 text-emerald-700", dot: "bg-emerald-400" },
  "Pending Review":{ cls: "bg-amber-100 text-amber-700",     dot: "bg-amber-400"   },
  "Delinquent":    { cls: "bg-red-100 text-red-700",         dot: "bg-red-400"     },
};

const TYPE_CFG: Record<PropertyType, { cls: string; icon: string }> = {
  Residential:  { cls: "bg-blue-100 text-blue-700",    icon: "🏠" },
  Commercial:   { cls: "bg-purple-100 text-purple-700",icon: "🏢" },
  Agricultural: { cls: "bg-green-100 text-green-700",  icon: "🌾" },
  Industrial:   { cls: "bg-orange-100 text-orange-700",icon: "🏭" },
  Special:      { cls: "bg-slate-100 text-slate-600",  icon: "⛪" },
};

export default function PropertyRegistration() {
  const { can, user } = useAuth();

  const canCreate = can("property.create");
  const canEdit   = can("property.edit");
  const canDelete = can("property.delete");
  const isReadOnly = !canCreate && !canEdit && !canDelete;

  const [properties,  setProperties]  = useState<Property[]>(INITIAL_PROPERTIES);
  const [search,      setSearch]      = useState("");
  const [typeFilter,  setTypeFilter]  = useState<"All" | PropertyType>("All");
  const [barangayFilter, setBarangayFilter] = useState("All");
  const [statusFilter, setStatusFilter] = useState<"All" | PropertyStatus>("All");
  const [modal,       setModal]       = useState<ModalMode>(null);
  const [selected,    setSelected]    = useState<Property | null>(null);
  const [form,        setForm]        = useState(EMPTY_FORM);
  const [toast,       setToast]       = useState<{ msg: string; type: "success" | "error" } | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const perPage = 8;

  const showToast = (msg: string, type: "success" | "error" = "success") => {
    setToast({ msg, type }); setTimeout(() => setToast(null), 3000);
  };

  const filtered = useMemo(() =>
    properties.filter((p) => {
      const mSearch = p.ownerName.toLowerCase().includes(search.toLowerCase()) ||
        p.id.toLowerCase().includes(search.toLowerCase()) ||
        p.barangay.toLowerCase().includes(search.toLowerCase()) ||
        p.lotNumber.toLowerCase().includes(search.toLowerCase());
      const mType   = typeFilter === "All" || p.propertyType === typeFilter;
      const mBarangay = barangayFilter === "All" || p.barangay === barangayFilter;
      const mStatus = statusFilter === "All" || p.status === statusFilter;
      return mSearch && mType && mBarangay && mStatus;
    }), [properties, search, typeFilter, barangayFilter, statusFilter]
  );

  const totalPages = Math.ceil(filtered.length / perPage);
  const paginated  = filtered.slice((currentPage - 1) * perPage, currentPage * perPage);

  const summary = useMemo(() => ({
    total: properties.length,
    totalMarket: properties.reduce((s, p) => s + p.marketValue, 0),
    totalAssessed: properties.reduce((s, p) => s + p.assessedValue, 0),
    anomalies: properties.filter((p) => p.anomaly).length,
  }), [properties]);

  const computeAssessed = (marketValue: number, type: PropertyType) => {
    return marketValue * (ASSESSMENT_LEVELS[type] / 100);
  };

  const openAdd    = () => { setForm(EMPTY_FORM); setSelected(null); setModal("add"); };
  const openEdit   = (p: Property) => { setSelected(p); setForm({ ownerName: p.ownerName, barangay: p.barangay, propertyType: p.propertyType, lotNumber: p.lotNumber, areaSqm: String(p.areaSqm), marketValue: String(p.marketValue), status: p.status }); setModal("edit"); };
  const openDelete = (p: Property) => { setSelected(p); setModal("delete"); };
  const openView   = (p: Property) => { setSelected(p); setModal("view"); };

  const handleSave = () => {
    if (!form.ownerName.trim() || !form.lotNumber.trim() || !form.marketValue || isNaN(Number(form.marketValue))) {
      showToast("Please fill in all required fields correctly.", "error"); return;
    }
    const mv = Number(form.marketValue);
    const al = ASSESSMENT_LEVELS[form.propertyType];
    const av = computeAssessed(mv, form.propertyType);

    if (modal === "add") {
      const typePrefix = form.propertyType.slice(0, 3).toUpperCase();
      const nextId = `${typePrefix}-${String(properties.filter(p => p.propertyType === form.propertyType).length + 1).padStart(4, "0")}`;
      const newProp: Property = {
        id: nextId, pin: `01-001-${nextId}-000-00-000`,
        ownerName: form.ownerName, barangay: form.barangay,
        propertyType: form.propertyType, lotNumber: form.lotNumber,
        areaSqm: Number(form.areaSqm) || 0, marketValue: mv,
        assessmentLevel: al, assessedValue: av,
        status: form.status, dateRegistered: new Date().toISOString().split("T")[0],
      };
      setProperties((p) => [newProp, ...p]);
      showToast("Property registered successfully.");
    } else if (modal === "edit" && selected) {
      setProperties((prev) => prev.map((p) => p.id === selected.id
        ? { ...p, ownerName: form.ownerName, barangay: form.barangay, propertyType: form.propertyType, lotNumber: form.lotNumber, areaSqm: Number(form.areaSqm) || p.areaSqm, marketValue: mv, assessmentLevel: al, assessedValue: av, status: form.status }
        : p
      ));
      showToast("Property record updated successfully.");
    }
    setModal(null); setCurrentPage(1);
  };

  const handleDelete = () => {
    if (!selected) return;
    setProperties((p) => p.filter((x) => x.id !== selected.id));
    showToast("Property record deleted."); setModal(null);
  };

  const marketVal = Number(form.marketValue);
  const previewAssessed = !isNaN(marketVal) && marketVal > 0
    ? computeAssessed(marketVal, form.propertyType) : 0;

  return (
    <div className="space-y-5 max-w-7xl mx-auto">
      {/* Toast */}
      {toast && (
        <div className={`fixed top-5 right-5 z-50 flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg border text-sm font-medium ${toast.type === "success" ? "bg-emerald-50 border-emerald-200 text-emerald-800" : "bg-red-50 border-red-200 text-red-800"}`}>
          {toast.type === "success" ? <CheckCircle className="h-4 w-4 text-emerald-500" /> : <AlertCircle className="h-4 w-4 text-red-500" />}
          {toast.msg}
        </div>
      )}

      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-slate-900 tracking-tight">Property Registry</h1>
          <p className="text-sm text-slate-500 mt-1">Manage property records, assessments, and owner information for the Davao Region LGU jurisdiction.</p>
        </div>
        <div className="flex gap-2">
          <button className="px-4 py-2 bg-white border border-slate-200 text-slate-700 rounded-lg text-sm font-medium hover:bg-slate-50 shadow-sm flex items-center gap-2">
            <Download className="h-4 w-4" /> Export
          </button>
          {canCreate && (
            <button onClick={openAdd} className="px-4 py-2 text-white rounded-lg text-sm font-medium transition-colors shadow-sm flex items-center gap-2"
              style={{ backgroundColor: "#0d2137" }}
              onMouseEnter={(e) => e.currentTarget.style.backgroundColor = "#1e3a5f"}
              onMouseLeave={(e) => e.currentTarget.style.backgroundColor = "#0d2137"}>
              <Plus className="h-4 w-4" /> Register Property
            </button>
          )}
        </div>
      </div>

      {/* Banners */}
      {isReadOnly && <ReadOnlyBanner message="Read-Only Mode — Auditors can view and inspect property records but cannot add, edit, or delete any data." />}
      {user?.role === "Staff" && <LimitedAccessBanner message="Staff Mode — You can register new properties. Editing and deleting records require Accountant or Admin access." />}

      {/* Summary Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { label: "Total Properties", value: summary.total.toLocaleString(), sub: "in registry",    color: "border-l-blue-500",    bg: "bg-blue-50 text-blue-600" },
          { label: "Total Market Value", value: fmtShort(summary.totalMarket), sub: "combined",     color: "border-l-emerald-500", bg: "bg-emerald-50 text-emerald-600" },
          { label: "Total Assessed Value",value: fmtShort(summary.totalAssessed), sub: "for taxation", color: "border-l-purple-500", bg: "bg-purple-50 text-purple-600" },
          { label: "Anomalies Detected", value: String(summary.anomalies), sub: "flagged by AI",   color: "border-l-amber-500",   bg: "bg-amber-50 text-amber-600" },
        ].map(({ label, value, sub, color, bg }) => (
          <div key={label} className={`bg-white p-4 rounded-xl border border-slate-200 border-l-4 shadow-sm flex items-center gap-3 ${color}`}>
            <div className={`p-2 rounded-lg flex-shrink-0 ${bg}`}>
              <Home className="h-4 w-4" />
            </div>
            <div>
              <p className="text-xs text-slate-500">{label}</p>
              <p className="text-xl font-bold text-slate-900">{value}</p>
              <p className="text-[10px] text-slate-400">{sub}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Main Table */}
      <div className="bg-white border border-slate-200 shadow-sm rounded-xl overflow-hidden">
        {/* Filter Bar */}
        <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex flex-wrap gap-3 items-center justify-between">
          <div className="relative flex-1 min-w-48 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <input type="text" placeholder="Search owner, property ID, barangay..."
              value={search} onChange={(e) => { setSearch(e.target.value); setCurrentPage(1); }}
              className="pl-9 pr-4 py-2 w-full border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white" />
          </div>
          <div className="flex gap-2 flex-wrap">
            <select value={typeFilter} onChange={(e) => { setTypeFilter(e.target.value as any); setCurrentPage(1); }}
              className="px-3 py-2 border border-slate-200 rounded-lg text-sm bg-white text-slate-600 focus:outline-none">
              <option value="All">All Types</option>
              {PROPERTY_TYPES.map((t) => <option key={t}>{t}</option>)}
            </select>
            <select value={barangayFilter} onChange={(e) => { setBarangayFilter(e.target.value); setCurrentPage(1); }}
              className="px-3 py-2 border border-slate-200 rounded-lg text-sm bg-white text-slate-600 focus:outline-none">
              {BARANGAYS.map((b) => <option key={b}>{b}</option>)}
            </select>
            <select value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value as any); setCurrentPage(1); }}
              className="px-3 py-2 border border-slate-200 rounded-lg text-sm bg-white text-slate-600 focus:outline-none">
              <option value="All">All Status</option>
              <option>Registered</option><option>Pending Review</option><option>Delinquent</option>
            </select>
            <button className="p-2 border border-slate-200 rounded-lg text-slate-500 hover:bg-slate-50"><Filter className="h-4 w-4" /></button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm whitespace-nowrap">
            <thead className="bg-slate-50 text-slate-500 text-xs uppercase font-medium border-b border-slate-200 tracking-wider">
              <tr>
                <th className="px-5 py-3.5">Property ID</th>
                <th className="px-5 py-3.5">Owner Name</th>
                <th className="px-5 py-3.5">Barangay</th>
                <th className="px-5 py-3.5">Type</th>
                <th className="px-5 py-3.5 text-right">Market Value</th>
                <th className="px-5 py-3.5 text-right">Assessed Value</th>
                <th className="px-5 py-3.5">Status</th>
                <th className="px-5 py-3.5 text-center">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {paginated.length === 0 ? (
                <tr><td colSpan={8} className="px-5 py-12 text-center text-slate-400 text-sm">No property records found.</td></tr>
              ) : (
                paginated.map((prop) => (
                  <tr key={prop.id}
                    className={`hover:bg-slate-50 transition-colors group ${prop.anomaly ? "bg-amber-50/30" : ""}`}>
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-2">
                        <span className="text-blue-700 text-xs font-mono font-bold">{prop.id}</span>
                        {prop.anomaly && (
                          <span title={prop.anomaly}>
                            <AlertTriangle className="h-3.5 w-3.5 text-amber-500" />
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      <p className="font-medium text-slate-900">{prop.ownerName}</p>
                      <p className="text-xs text-slate-400">Lot: {prop.lotNumber}</p>
                    </td>
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-1.5 text-xs text-slate-600">
                        <MapPin className="h-3.5 w-3.5 text-slate-400 flex-shrink-0" />
                        {prop.barangay}
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${TYPE_CFG[prop.propertyType].cls}`}>
                        {TYPE_CFG[prop.propertyType].icon} {prop.propertyType}
                      </span>
                    </td>
                    <td className="px-5 py-3.5 text-right text-xs text-slate-600">{fmt(prop.marketValue)}</td>
                    <td className="px-5 py-3.5 text-right text-xs font-bold text-slate-900">{fmt(prop.assessedValue)}</td>
                    <td className="px-5 py-3.5">
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold ${STATUS_CFG[prop.status].cls}`}>
                        <span className={`w-1.5 h-1.5 rounded-full ${STATUS_CFG[prop.status].dot}`} />
                        {prop.status}
                      </span>
                    </td>
                    <td className="px-5 py-3.5 text-center">
                      <div className="flex justify-center gap-1.5 opacity-0 group-hover:opacity-100 transition-opacity">
                        <button onClick={() => openView(prop)} className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors" title="View Details">
                          <Eye className="h-3.5 w-3.5" />
                        </button>
                        {canEdit && (
                          <button onClick={() => openEdit(prop)} className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors" title="Edit">
                            <Edit3 className="h-3.5 w-3.5" />
                          </button>
                        )}
                        {canDelete && (
                          <button onClick={() => openDelete(prop)} className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors" title="Delete">
                            <Trash2 className="h-3.5 w-3.5" />
                          </button>
                        )}
                        {/* Auditor sees anomaly flag info only */}
                        {isReadOnly && prop.anomaly && (
                          <span title={`Anomaly: ${prop.anomaly}`}
                            className="p-1.5 text-amber-500 bg-amber-50 rounded border border-amber-200 cursor-default" >
                            <AlertTriangle className="h-3.5 w-3.5" />
                          </span>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {!canEdit && !canDelete && (
          <div className="px-5 py-2.5 border-t border-slate-100 bg-amber-50 flex items-center gap-2 text-xs text-amber-700 border-amber-200">
            <Lock className="h-3.5 w-3.5 text-amber-600 flex-shrink-0" />
            <span className="font-medium">Read-Only Access — No modifications permitted for {user?.role} role</span>
          </div>
        )}

        {/* Anomaly Legend */}
        <div className="px-5 py-2.5 border-t border-amber-100 bg-amber-50/40 flex items-center gap-2 text-xs text-amber-700">
          <AlertTriangle className="h-3.5 w-3.5 flex-shrink-0 text-amber-500" />
          Rows highlighted in amber contain AI-flagged anomalies requiring review.
        </div>

        {/* Pagination */}
        <div className="p-4 border-t border-slate-200 bg-slate-50/50 flex items-center justify-between text-sm text-slate-500">
          <span className="text-xs">Showing {Math.min((currentPage - 1) * perPage + 1, filtered.length)}–{Math.min(currentPage * perPage, filtered.length)} of {filtered.length}</span>
          <div className="flex gap-1">
            <button onClick={() => setCurrentPage((p) => Math.max(1, p - 1))} disabled={currentPage === 1} className="px-3 py-1.5 border border-slate-200 rounded-lg text-xs hover:bg-white disabled:opacity-40">Prev</button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
              <button key={p} onClick={() => setCurrentPage(p)} className={`px-3 py-1.5 rounded-lg text-xs ${p === currentPage ? "text-white" : "border border-slate-200 hover:bg-white"}`}
                style={p === currentPage ? { backgroundColor: "#0d2137" } : {}}>
                {p}
              </button>
            ))}
            <button onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages || totalPages === 0} className="px-3 py-1.5 border border-slate-200 rounded-lg text-xs hover:bg-white disabled:opacity-40">Next</button>
          </div>
        </div>
      </div>

      {/* ── Add / Edit Modal ── */}
      {(modal === "add" || modal === "edit") && (canCreate || canEdit) && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200 bg-slate-50 sticky top-0">
              <div>
                <h3 className="text-slate-900">{modal === "add" ? "Register New Property" : "Edit Property Record"}</h3>
                <p className="text-xs text-slate-500 mt-0.5">{modal === "add" ? "Add property to the Davao Region registry" : `Editing: ${selected?.id}`}</p>
              </div>
              <button onClick={() => setModal(null)} className="p-1.5 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg">
                <X className="h-4 w-4" />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="sm:col-span-2">
                  <label className="block text-sm text-slate-700 mb-1.5">Owner Name <span className="text-red-500">*</span></label>
                  <input type="text" placeholder="Full legal name of property owner" value={form.ownerName}
                    onChange={(e) => setForm((f) => ({ ...f, ownerName: e.target.value }))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-sm text-slate-700 mb-1.5">Property Type <span className="text-red-500">*</span></label>
                  <select value={form.propertyType} onChange={(e) => setForm((f) => ({ ...f, propertyType: e.target.value as PropertyType }))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white">
                    {PROPERTY_TYPES.map((t) => <option key={t}>{t}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-sm text-slate-700 mb-1.5">Barangay <span className="text-red-500">*</span></label>
                  <select value={form.barangay} onChange={(e) => setForm((f) => ({ ...f, barangay: e.target.value }))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white">
                    {BARANGAYS.filter(b => b !== "All").map((b) => <option key={b}>{b}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-sm text-slate-700 mb-1.5">Lot Number / Location <span className="text-red-500">*</span></label>
                  <input type="text" placeholder="e.g. Lot 12, Blk 5" value={form.lotNumber}
                    onChange={(e) => setForm((f) => ({ ...f, lotNumber: e.target.value }))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-sm text-slate-700 mb-1.5">Area (sqm)</label>
                  <input type="number" placeholder="0" value={form.areaSqm}
                    onChange={(e) => setForm((f) => ({ ...f, areaSqm: e.target.value }))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-sm text-slate-700 mb-1.5">Market Value (₱) <span className="text-red-500">*</span></label>
                  <input type="number" placeholder="0.00" value={form.marketValue}
                    onChange={(e) => setForm((f) => ({ ...f, marketValue: e.target.value }))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-sm text-slate-700 mb-1.5">Status</label>
                  <select value={form.status} onChange={(e) => setForm((f) => ({ ...f, status: e.target.value as PropertyStatus }))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white">
                    <option>Registered</option><option>Pending Review</option><option>Delinquent</option>
                  </select>
                </div>
              </div>

              {/* Assessment Preview */}
              {marketVal > 0 && (
                <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg">
                  <p className="text-xs font-semibold text-blue-800 mb-3">Assessment Preview (Auto-Calculated)</p>
                  <div className="grid grid-cols-3 gap-4 text-center">
                    <div>
                      <p className="text-xs text-slate-500">Market Value</p>
                      <p className="text-sm font-bold text-slate-900">{fmt(marketVal)}</p>
                    </div>
                    <div>
                      <p className="text-xs text-slate-500">Assessment Level ({ASSESSMENT_LEVELS[form.propertyType]}%)</p>
                      <p className="text-sm font-bold text-blue-600">×{ASSESSMENT_LEVELS[form.propertyType]}%</p>
                    </div>
                    <div>
                      <p className="text-xs text-blue-600">Assessed Value</p>
                      <p className="text-sm font-bold text-blue-700">{fmt(previewAssessed)}</p>
                    </div>
                  </div>
                </div>
              )}
            </div>
            <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-slate-200 bg-slate-50">
              <button onClick={() => setModal(null)} className="px-4 py-2 border border-slate-200 rounded-lg text-sm text-slate-600 hover:bg-slate-100">Cancel</button>
              <button onClick={handleSave} className="px-5 py-2 text-white rounded-lg text-sm font-medium shadow-sm transition-colors"
                style={{ backgroundColor: "#0d2137" }}
                onMouseEnter={(e) => e.currentTarget.style.backgroundColor = "#1e3a5f"}
                onMouseLeave={(e) => e.currentTarget.style.backgroundColor = "#0d2137"}>
                {modal === "add" ? "Register Property" : "Save Changes"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── View Details Modal ── */}
      {modal === "view" && selected && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200 bg-slate-50">
              <div>
                <h3 className="text-slate-900">Property Details</h3>
                <p className="text-xs text-slate-400 mt-0.5 font-mono">{selected.id} · PIN: {selected.pin}</p>
              </div>
              <button onClick={() => setModal(null)} className="p-1.5 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg"><X className="h-4 w-4" /></button>
            </div>
            <div className="p-6 space-y-3">
              <div className={`inline-flex items-center gap-1 px-2.5 py-1 rounded text-xs font-bold ${TYPE_CFG[selected.propertyType].cls}`}>
                {TYPE_CFG[selected.propertyType].icon} {selected.propertyType}
              </div>
              {selected.anomaly && (
                <div className="flex items-start gap-2.5 p-3 bg-amber-50 border border-amber-200 rounded-lg">
                  <AlertTriangle className="h-4 w-4 text-amber-600 flex-shrink-0 mt-0.5" />
                  <div>
                    <p className="text-xs font-bold text-amber-800">AI Anomaly Flag</p>
                    <p className="text-xs text-amber-700 mt-0.5">{selected.anomaly} — Review required</p>
                  </div>
                </div>
              )}
              {[
                ["Owner Name", selected.ownerName],
                ["Barangay", selected.barangay],
                ["Lot Number", selected.lotNumber],
                ["Area", `${selected.areaSqm.toLocaleString()} sqm`],
                ["Market Value", fmt(selected.marketValue)],
                ["Assessment Level", `${selected.assessmentLevel}%`],
                ["Assessed Value", fmt(selected.assessedValue)],
                ["Status", selected.status],
                ["Date Registered", selected.dateRegistered],
              ].map(([l, v]) => (
                <div key={l} className="flex justify-between items-start py-2 border-b border-slate-100 last:border-0">
                  <span className="text-xs text-slate-500 w-36 flex-shrink-0">{l}</span>
                  <span className="text-sm font-medium text-slate-800 text-right">{v}</span>
                </div>
              ))}
            </div>
            <div className="flex gap-3 px-6 pb-6">
              <button onClick={() => setModal(null)} className="flex-1 py-2 border border-slate-200 rounded-lg text-sm text-slate-600 hover:bg-slate-50">Close</button>
              {canEdit && <button onClick={() => openEdit(selected)} className="flex-1 py-2 text-white rounded-lg text-sm font-medium" style={{ backgroundColor: "#0d2137" }}>Edit Record</button>}
            </div>
          </div>
        </div>
      )}

      {/* ── Delete Modal ── */}
      {modal === "delete" && selected && canDelete && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden">
            <div className="p-6 text-center">
              <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <Trash2 className="h-6 w-6 text-red-600" />
              </div>
              <h3 className="text-slate-900 mb-2">Delete Property Record?</h3>
              <p className="text-sm text-slate-600 mb-2">{selected.ownerName} — <span className="font-mono font-bold">{selected.id}</span></p>
              <p className="text-xs text-red-500">This action cannot be undone. All associated records may be affected.</p>
            </div>
            <div className="flex gap-3 px-6 pb-6">
              <button onClick={() => setModal(null)} className="flex-1 px-4 py-2 border border-slate-200 rounded-lg text-sm text-slate-600 hover:bg-slate-50">Cancel</button>
              <button onClick={handleDelete} className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700">Yes, Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}