import {
  Home, TrendingUp, AlertCircle, CheckCircle2, ArrowRight,
  CalendarDays, CreditCard, ArrowUpRight, AlertTriangle,
  Building2, Zap, Brain, TrendingDown, Lock, ClipboardList, FileDown,
} from "lucide-react";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, AreaChart, Area,
  PieChart, Pie, Cell, ReferenceLine,
} from "recharts";
import { useAuth } from "../context/AuthContext";
import { useNavigate } from "react-router";

// ─── Mock data ────────────────────────────────────────────────────────────────
const monthlyCollection: { name: string; collected: number; target: number }[] = [];

const paymentStatus: { name: string; value: number; color: string }[] = [];

const mlPrediction: { name: string; predicted: number }[] = [];

const ALERTS: { id: number; title: string; desc: string; type: string; time: string }[] = [];

const RECENT_PAYMENTS: { id: string; owner: string; property: string; amount: number; date: string }[] = [];

const ANOMALY_PROPERTIES: { id: string; owner: string; barangay: string; assessed: string; flag: string }[] = [];

const fmt     = (val: number) => `₱ ${val.toLocaleString("en-PH", { minimumFractionDigits: 0 })}`;
const fmtM    = (val: number) => val >= 1000000 ? `₱${(val / 1000000).toFixed(1)}M` : `₱${(val / 1000).toFixed(0)}K`;

// ─── Compliance Arc Gauge ─────────────────────────────────────────────────────
function ComplianceArc({ value, target }: { value: number; target: number }) {
  const cx = 70, cy = 56, r = 44, sw = 10;

  // Convert angle (standard math, CCW from right) to SVG coords (y-down)
  const toXY = (deg: number) => ({
    x: +(cx + r * Math.cos((deg * Math.PI) / 180)).toFixed(2),
    y: +(cy - r * Math.sin((deg * Math.PI) / 180)).toFixed(2),
  });

  // Background arc: from left (≈180°) through top (90°) to right (≈0°)
  // Split into two quarter-arcs to avoid the 180° degenerate ambiguity
  const s   = toXY(179.9);   // left endpoint
  const mid = toXY(90);      // topmost point
  const e   = toXY(0.1);     // right endpoint
  const bgPath = `M ${s.x} ${s.y} A ${r} ${r} 0 0 0 ${mid.x} ${mid.y} A ${r} ${r} 0 0 0 ${e.x} ${e.y}`;

  // Value arc: from left to value angle, counterclockwise (sweep-flag=0)
  // Angle decreases as value% increases: 180° → 0° maps to 0% → 100%
  const vDeg  = 180 - 180 * (Math.min(value, 99.5) / 100);
  const vPt   = toXY(vDeg);
  const vPath = value > 0.5
    ? `M ${s.x} ${s.y} A ${r} ${r} 0 0 0 ${vPt.x} ${vPt.y}`
    : "";

  // Target marker position
  const tPt = toXY(180 - 180 * (target / 100));
  const gap = (target - value).toFixed(1);

  return (
    <svg viewBox="0 0 140 72" className="w-full" aria-label={`Compliance gauge: ${value}% of ${target}% target`}>
      {/* Track */}
      <path d={bgPath} fill="none" stroke="#e2e8f0" strokeWidth={sw} strokeLinecap="butt" />
      {/* Value fill */}
      {vPath && <path d={vPath} fill="none" stroke="#1e40af" strokeWidth={sw} strokeLinecap="butt" />}

      {/* Target marker — red dot */}
      <circle cx={tPt.x} cy={tPt.y} r={5} fill="#ef4444" />
      {/* Small target label near the dot */}
      <text
        x={tPt.x + (tPt.x > cx ? 7 : -7)}
        y={tPt.y - 5}
        textAnchor={tPt.x > cx ? "start" : "end"}
        fontSize="7"
        fontWeight="700"
        fill="#ef4444"
      >
        {target}%
      </text>

      {/* Centre: large percentage */}
      <text x={cx} y={cy - 9} textAnchor="middle" fontSize="19" fontWeight="800" fill="#0f172a">
        {value}%
      </text>
      {/* Gap note */}
      <text x={cx} y={cy + 5} textAnchor="middle" fontSize="7.5" fill="#ef4444">
        {gap}% below target
      </text>

      {/* Axis labels */}
      <text x={s.x + 3} y={cy + 14} fontSize="7.5" fill="#94a3b8" textAnchor="start">0%</text>
      <text x={e.x - 3} y={cy + 14} fontSize="7.5" fill="#94a3b8" textAnchor="end">100%</text>
    </svg>
  );
}

// ─── Bar chart custom tooltip ─────────────────────────────────────────────────
const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload && payload.length) {
    return (
      <div className="bg-white border border-slate-200 rounded-lg shadow-lg p-3 text-xs">
        <p className="font-semibold text-slate-700 mb-1">{label}</p>
        {payload.map((p: any) => (
          <p key={p.name} style={{ color: p.color ?? p.fill }}>
            {p.name}: {fmt(p.value)}
          </p>
        ))}
      </div>
    );
  }
  return null;
};

// ─── Locked AI banner (for Staff & Auditor) ───────────────────────────────────
function LockedAIBanner() {
  return (
    <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-slate-200 flex items-center gap-3" style={{ backgroundColor: "#1e293b" }}>
        <div className="p-2 rounded-lg bg-slate-700">
          <Brain className="h-4 w-4 text-slate-400" />
        </div>
        <div>
          <h3 className="text-slate-400">AI Intelligence Module</h3>
          <p className="text-slate-500 text-xs mt-0.5">Machine Learning predictions and anomaly detection</p>
        </div>
        <span className="ml-auto px-2.5 py-1 bg-slate-700 text-slate-400 text-xs font-bold rounded-full border border-slate-600">
          RESTRICTED
        </span>
      </div>
      <div className="flex flex-col items-center justify-center py-10 px-6 text-center">
        <div className="w-14 h-14 rounded-full bg-slate-100 flex items-center justify-center mb-4">
          <Lock className="h-6 w-6 text-slate-400" />
        </div>
        <p className="text-sm font-semibold text-slate-700 mb-1">
          AI Intelligence — Restricted Access
        </p>
        <p className="text-xs text-slate-400 max-w-sm leading-relaxed">
          The AI predictions and anomaly detection module is available to{" "}
          <span className="font-medium text-slate-600">Accountant level and above</span>.
          Contact your system administrator to request access.
        </p>
        <div className="mt-4 flex items-center gap-2 px-4 py-2 bg-slate-50 border border-slate-200 rounded-lg">
          <div className="flex gap-0.5">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className={`h-1.5 w-6 rounded-full ${i <= 2 ? "bg-slate-300" : "bg-slate-100"}`} />
            ))}
          </div>
          <span className="text-[10px] text-slate-400">Access Level 3+ required (Accountant or Admin)</span>
        </div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN DASHBOARD
// ─────────────────────────────────────────────────────────────────────────────
export default function Dashboard() {
  const { user, can } = useAuth();
  const navigate = useNavigate();
  const today = new Date().toLocaleDateString("en-PH", {
    weekday: "long", year: "numeric", month: "long", day: "numeric",
  });
  const totalProperties = paymentStatus.reduce((s, d) => s + d.value, 0);

  // ── Role-based gates ────────────────────────────────────────────────────────
  // AI panel - Admin & Accountant get full panel; Auditor gets anomaly-only; Staff gets locked
  const canViewAI = can("reporting.generate"); // Admin & Accountant have this
  const isAuditor = user?.role === "Auditor";

  // Register Property button - use permission check
  const canRegisterProperty = can("property.create");
  const canExport = can("reporting.export");

  return (
    <div className="space-y-6 max-w-7xl mx-auto">

      {/* ── Page Header ───────────────────────────────────────────────────────── */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-slate-900 tracking-tight">Executive Dashboard</h1>
          <p className="text-sm text-slate-500 mt-1">
            Welcome back, <span className="font-medium text-slate-700">{user?.name}</span> · {today}
          </p>
        </div>
        <div className="flex gap-2">
          {canExport ? (
            <button className="px-4 py-2 bg-white border border-slate-200 text-slate-700 rounded-lg text-sm font-medium hover:bg-slate-50 transition-colors shadow-sm">
              Export Report
            </button>
          ) : (
            <button disabled className="px-4 py-2 bg-slate-100 border border-slate-200 text-slate-400 rounded-lg text-sm font-medium cursor-not-allowed shadow-sm">
              Export Report
            </button>
          )}
          {canRegisterProperty && (
            <button
              className="px-4 py-2 text-white rounded-lg text-sm font-medium transition-colors shadow-sm flex items-center gap-2"
              style={{ backgroundColor: "#0d2137" }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = "#1e3a5f")}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = "#0d2137")}
            >
              Register Property <ArrowRight className="h-4 w-4" />
            </button>
          )}
        </div>
      </div>

      {/* ── KPI Cards ────────────────────────────────────────────────────────── */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">

        {/* Total Properties */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-sm hover:shadow-md transition-shadow group">
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-start gap-3 flex-1 min-w-0">
              <div className="p-3 bg-blue-50 rounded-xl text-blue-600 group-hover:bg-blue-100 transition-colors flex-shrink-0">
                <Home className="h-5 w-5" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-xs font-medium text-slate-500 uppercase tracking-wider">Total Properties</p>
                <p className="text-xs text-emerald-600 font-medium mt-2 flex items-center gap-1">
                  <ArrowUpRight className="h-3 w-3" /> +142 this quarter
                </p>
                <div className="mt-3 h-1 bg-slate-100 rounded-full overflow-hidden">
                  <div className="h-full bg-blue-500 rounded-full" style={{ width: "76%" }} />
                </div>
                <p className="text-[10px] text-slate-400 mt-1">76% registered in system</p>
              </div>
            </div>
            <div className="text-right flex-shrink-0">
              <h3 className="text-3xl font-bold text-slate-900">{totalProperties.toLocaleString()}</h3>
            </div>
          </div>
        </div>

        {/* Tax Collected */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-sm hover:shadow-md transition-shadow group border-l-4 border-l-emerald-400">
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-start gap-3 flex-1 min-w-0">
              <div className="p-3 bg-emerald-50 rounded-xl text-emerald-600 group-hover:bg-emerald-100 transition-colors flex-shrink-0">
                <TrendingUp className="h-5 w-5" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-xs font-medium text-slate-500 uppercase tracking-wider">Tax Collected YTD</p>
                <p className="text-xs text-emerald-600 font-medium mt-2 flex items-center gap-1">
                  <ArrowUpRight className="h-3 w-3" /> +8.4% vs last year
                </p>
                <div className="mt-3 h-1 bg-slate-100 rounded-full overflow-hidden">
                  <div className="h-full bg-emerald-500 rounded-full" style={{ width: "56%" }} />
                </div>
                <p className="text-[10px] text-slate-400 mt-1">56% of annual target (₱30M)</p>
              </div>
            </div>
            <div className="text-right flex-shrink-0">
              <h3 className="text-3xl font-bold text-slate-900">₱16.84M</h3>
            </div>
          </div>
        </div>

        {/* Pending Payments */}
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-sm hover:shadow-md transition-shadow border-l-4 border-l-amber-400 group">
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-start gap-3 flex-1 min-w-0">
              <div className="p-3 bg-amber-50 rounded-xl text-amber-600 group-hover:bg-amber-100 transition-colors flex-shrink-0">
                <AlertCircle className="h-5 w-5" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-xs font-medium text-slate-500 uppercase tracking-wider">Pending Payments</p>
                <p className="text-xs text-amber-600 font-medium mt-2 flex items-center gap-1">
                  <AlertTriangle className="h-3 w-3" /> 272 past due date
                </p>
                <div className="mt-3 flex gap-1">
                  {Array.from({ length: 10 }, (_, i) => (
                    <div key={i} className={`flex-1 h-1.5 rounded-full ${i < 7 ? "bg-amber-400" : "bg-slate-100"}`} />
                  ))}
                </div>
                <p className="text-[10px] text-slate-400 mt-1">734 unpaid + 272 late</p>
              </div>
            </div>
            <div className="text-right flex-shrink-0">
              <h3 className="text-3xl font-bold text-slate-900">1,006</h3>
            </div>
          </div>
        </div>

        {/* Issue 3: Compliance Rate — arc gauge ────────────��──────────────────── */}
        <div className="bg-white px-5 pt-4 pb-3 rounded-xl border border-slate-200 shadow-sm hover:shadow-md transition-shadow border-l-4 border-l-blue-400 group">
          <div className="flex justify-between items-start">
            <p className="text-xs font-medium text-slate-500 uppercase tracking-wider">Compliance Rate</p>
            <div className="p-2.5 bg-blue-50 rounded-xl text-blue-600 group-hover:bg-blue-100 transition-colors flex-shrink-0">
              <CheckCircle2 className="h-4 w-4" />
            </div>
          </div>

          {/* SVG Gauge */}
          <div className="mt-1">
            <ComplianceArc value={73.8} target={85} />
          </div>

          {/* Footer stats */}
          <div className="flex items-center justify-between text-[10px] mt-0.5">
            <span className="text-slate-400">2,841 / 3,847 paid</span>
            <span className="text-red-500 font-semibold">Q4 2026 target: 85%</span>
          </div>
        </div>
      </div>

      {/* ── Charts Row ────────────────────────────────────────────────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

        {/* Issue 2: Monthly Tax Collection — Target now a visible dashed ReferenceLine */}
        <div className="lg:col-span-2 bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
          <div className="flex items-center justify-between mb-5">
            <div>
              <h3 className="text-slate-900">Monthly Tax Collection</h3>
              <p className="text-xs text-slate-400 mt-0.5">Real Property Tax collected vs. monthly target (₱3M)</p>
            </div>
            <div className="flex items-center gap-3">
              {/* Manual legend for clarity */}
              <div className="flex items-center gap-4 text-xs text-slate-500">
                <span className="flex items-center gap-1.5">
                  <span className="inline-block w-3 h-3 rounded-sm" style={{ backgroundColor: "#0d2137" }} />
                  Collected
                </span>
                <span className="flex items-center gap-1.5">
                  <svg width="16" height="10">
                    <line x1="0" y1="5" x2="16" y2="5" stroke="#f97316" strokeWidth="2" strokeDasharray="4 2" />
                  </svg>
                  Target
                </span>
              </div>
              <select className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 text-slate-500 bg-white focus:outline-none">
                <option>2026</option><option>2025</option>
              </select>
            </div>
          </div>
          <div style={{ height: 260 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={monthlyCollection} margin={{ top: 16, right: 10, left: 0, bottom: 0 }} barCategoryGap="30%">
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                <XAxis dataKey="name" axisLine={false} tickLine={false} tick={{ fill: "#94a3b8", fontSize: 11 }} dy={8} />
                <YAxis axisLine={false} tickLine={false} tick={{ fill: "#94a3b8", fontSize: 11 }} tickFormatter={fmtM} />
                <Tooltip content={<CustomTooltip />} cursor={{ fill: "rgba(248,250,252,0.8)" }} />

                {/* Dashed reference line for the monthly target (constant 3,000,000) */}
                <ReferenceLine
                  y={3000000}
                  stroke="#f97316"
                  strokeDasharray="6 3"
                  strokeWidth={1.8}
                  label={{
                    value: "Target ₱3M",
                    position: "insideTopRight",
                    fontSize: 10,
                    fill: "#f97316",
                    fontWeight: "600",
                    dy: -4,
                  }}
                />

                {/* Single collected bar — dark navy, clearly visible */}
                <Bar
                  dataKey="collected"
                  fill="#0d2137"
                  radius={[4, 4, 0, 0]}
                  barSize={36}
                  name="Collected"
                />
              </BarChart>
            </ResponsiveContainer>
          </div>
          {/* Below-chart legend clarification */}
          <p className="text-[10px] text-slate-400 mt-2 text-center">
            Orange dashed line = monthly target · Bars above line = target exceeded
          </p>
        </div>

        {/* Paid vs Unpaid Pie */}
        <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
          <div className="mb-5">
            <h3 className="text-slate-900">Payment Status</h3>
            <p className="text-xs text-slate-400 mt-0.5">Property distribution by payment status</p>
          </div>
          <div style={{ height: 180 }}>
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie data={paymentStatus} cx="50%" cy="50%" innerRadius={50} outerRadius={80} dataKey="value" paddingAngle={3}>
                  {paymentStatus.map((entry, index) => (
                    <Cell key={index} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(value: number, name: string) => [`${value.toLocaleString()} properties`, name]}
                  contentStyle={{ fontSize: "11px", borderRadius: "8px", border: "1px solid #e2e8f0" }}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <div className="space-y-2 mt-2">
            {paymentStatus.map((item) => (
              <div key={item.name} className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full" style={{ backgroundColor: item.color }} />
                  <span className="text-xs text-slate-600">{item.name}</span>
                </div>
                <div className="text-right">
                  <span className="text-sm font-bold text-slate-900">{item.value.toLocaleString()}</span>
                  <span className="text-xs text-slate-400 ml-1">({((item.value / totalProperties) * 100).toFixed(1)}%)</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* ── Issue 4: AI Intelligence Panel — gated by role ─────────────────────── */}
      {canViewAI ? (
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="px-5 py-4 border-b border-slate-200 flex items-center gap-3" style={{ backgroundColor: "#0d2137" }}>
            <div className="p-2 rounded-lg bg-blue-600/30">
              <Brain className="h-4 w-4 text-blue-300" />
            </div>
            <div>
              <h3 className="text-white">AI Intelligence Module</h3>
              <p className="text-blue-300 text-xs mt-0.5">Machine Learning predictions and anomaly detection</p>
            </div>
            <span className="ml-auto px-2.5 py-1 bg-blue-600/30 text-blue-300 text-xs font-bold rounded-full border border-blue-400/30">
              BETA
            </span>
          </div>
          <div className="grid grid-cols-1 lg:grid-cols-2 divide-y lg:divide-y-0 lg:divide-x divide-slate-200">
            {/* Predicted Revenue */}
            <div className="p-5">
              <div className="flex items-center gap-2 mb-4">
                <TrendingUp className="h-4 w-4 text-blue-600" />
                <h4 className="text-sm font-semibold text-slate-800">Predicted Tax Revenue (Aug–Dec 2026)</h4>
              </div>
              <div style={{ height: 180 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={mlPrediction} margin={{ top: 5, right: 10, left: 0, bottom: 0 }}>
                    <defs>
                      <linearGradient id="predGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%"  stopColor="#3b82f6" stopOpacity={0.2} />
                        <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                    <XAxis dataKey="name" axisLine={false} tickLine={false} tick={{ fill: "#94a3b8", fontSize: 11 }} />
                    <YAxis axisLine={false} tickLine={false} tick={{ fill: "#94a3b8", fontSize: 11 }} tickFormatter={fmtM} />
                    <Tooltip
                      formatter={(value: number) => [fmtM(value), "Predicted"]}
                      contentStyle={{ fontSize: "11px", borderRadius: "8px", border: "1px solid #e2e8f0" }}
                    />
                    <Area type="monotone" dataKey="predicted" stroke="#3b82f6" strokeWidth={2.5}
                      strokeDasharray="6 3" fill="url(#predGrad)"
                      dot={{ r: 4, fill: "#3b82f6" }} name="Predicted Revenue" />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
              <div className="mt-3 p-3 bg-blue-50 rounded-lg border border-blue-100">
                <div className="flex items-center gap-2">
                  <Zap className="h-3.5 w-3.5 text-blue-600 flex-shrink-0" />
                  <p className="text-xs text-blue-700 font-medium">
                    Projected Annual Total: <span className="font-bold">₱33.2M</span> — 10.7% above target
                  </p>
                </div>
              </div>
            </div>

            {/* Anomaly Detection */}
            <div className="p-5">
              <div className="flex items-center gap-2 mb-4">
                <AlertTriangle className="h-4 w-4 text-amber-500" />
                <h4 className="text-sm font-semibold text-slate-800">Anomaly Detection Alerts</h4>
                <span className="ml-auto px-2 py-0.5 bg-amber-100 text-amber-700 text-[10px] font-bold rounded-full">
                  {ANOMALY_PROPERTIES.length} flagged
                </span>
              </div>
              <div className="space-y-3">
                {ANOMALY_PROPERTIES.map((prop) => (
                  <div key={prop.id} className="flex items-start gap-3 p-3 rounded-lg border border-amber-200 bg-amber-50/50">
                    <div className="p-1.5 bg-amber-100 rounded-md flex-shrink-0">
                      <AlertTriangle className="h-3.5 w-3.5 text-amber-600" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-xs font-bold text-amber-800 font-mono">{prop.id}</span>
                        <span className="text-[10px] bg-amber-200 text-amber-800 px-1.5 py-0.5 rounded font-medium">{prop.flag}</span>
                      </div>
                      <p className="text-xs text-slate-600 mt-0.5">{prop.owner} · {prop.barangay}</p>
                      <p className="text-xs font-semibold text-slate-800 mt-0.5">Assessed: {prop.assessed}</p>
                    </div>
                    <button className="flex-shrink-0 px-2 py-1 text-[10px] font-medium text-amber-700 border border-amber-300 rounded-lg hover:bg-amber-100 transition-colors">
                      Review
                    </button>
                  </div>
                ))}
              </div>
              <div className="mt-3 p-3 bg-slate-50 rounded-lg border border-slate-200">
                <p className="text-xs text-slate-500 flex items-center gap-1.5">
                  <Brain className="h-3.5 w-3.5 text-slate-400" />
                  Model trained on 3,847 property records · Last updated: Apr 1, 2026
                </p>
              </div>
            </div>
          </div>
        </div>
      ) : isAuditor ? (
        /* Auditor sees Anomaly Detection only — their key tool */
        <div className="bg-white rounded-xl border border-amber-200 shadow-sm overflow-hidden">
          <div className="px-5 py-4 border-b border-amber-200 flex items-center gap-3 bg-amber-50">
            <div className="p-2 rounded-lg bg-amber-100">
              <AlertTriangle className="h-4 w-4 text-amber-700" />
            </div>
            <div>
              <h3 className="text-amber-900">Anomaly Detection — Flagged Records</h3>
              <p className="text-amber-600 text-xs mt-0.5">AI-flagged property records requiring audit verification</p>
            </div>
            <span className="ml-auto px-2.5 py-1 bg-amber-100 text-amber-700 text-xs font-bold rounded-full border border-amber-300">
              AUDITOR VIEW
            </span>
          </div>
          <div className="grid grid-cols-1 lg:grid-cols-3 divide-y lg:divide-y-0 lg:divide-x divide-amber-100">
            {ANOMALY_PROPERTIES.map((prop) => (
              <div key={prop.id} className="p-5 flex items-start gap-3">
                <div className="p-2 bg-amber-100 rounded-lg flex-shrink-0 mt-0.5">
                  <AlertTriangle className="h-4 w-4 text-amber-600" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 flex-wrap mb-1">
                    <span className="text-xs font-bold text-amber-800 font-mono">{prop.id}</span>
                    <span className="text-[10px] bg-amber-200 text-amber-800 px-1.5 py-0.5 rounded font-semibold">{prop.flag}</span>
                  </div>
                  <p className="text-sm font-semibold text-slate-800">{prop.owner}</p>
                  <p className="text-xs text-slate-500">{prop.barangay}</p>
                  <div className="mt-2 flex items-center justify-between">
                    <p className="text-xs text-slate-600">Assessed: <span className="font-bold text-slate-800">{prop.assessed}</span></p>
                    <span className="text-[10px] text-amber-600 font-medium border border-amber-300 bg-amber-50 px-1.5 py-0.5 rounded">Pending Review</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
          <div className="px-5 py-3 border-t border-amber-100 bg-amber-50/50 flex items-center gap-2">
            <Lock className="h-3.5 w-3.5 text-amber-600" />
            <p className="text-xs text-amber-700">
              Read-only view · Cross-reference with <span className="font-semibold">Audit Support</span> and <span className="font-semibold">Property Registry</span> for full validation
            </p>
          </div>
        </div>
      ) : (
        /* Staff sees locked banner */
        <LockedAIBanner />
      )}

      {/* ── Bottom: Alerts + Recent Payments + Quick Actions ─────────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

        {/* Alerts */}
        <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="px-5 py-4 border-b border-slate-200 bg-slate-50/50 flex justify-between items-center">
            <h3 className="text-slate-900 flex items-center gap-2">
              <CalendarDays className="h-4 w-4 text-blue-600" /> Deadlines & Alerts
            </h3>
            <button className="text-xs text-blue-600 font-medium hover:text-blue-700">View All →</button>
          </div>
          <div className="divide-y divide-slate-100">
            {ALERTS.map((alert) => (
              <div key={alert.id} className={`p-4 hover:bg-slate-50 transition-colors flex items-start gap-4 group ${alert.type === "critical" ? "border-l-2 border-l-red-500" : ""}`}>
                <div className={`mt-0.5 p-2 rounded-full flex-shrink-0 ${alert.type === "critical" ? "bg-red-100 text-red-600" : alert.type === "deadline" ? "bg-amber-100 text-amber-600" : "bg-blue-100 text-blue-600"}`}>
                  {alert.type === "critical" ? <AlertTriangle className="h-4 w-4" /> :
                   alert.type === "deadline" ? <CalendarDays  className="h-4 w-4" /> :
                                               <Brain         className="h-4 w-4" />}
                </div>
                <div className="flex-1 min-w-0">
                  <h4 className={`text-sm font-medium ${alert.type === "critical" ? "text-red-700" : "text-slate-900"}`}>
                    {alert.title}
                  </h4>
                  <p className="text-xs text-slate-500 mt-0.5">{alert.desc}</p>
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                  <span className={`text-xs font-medium ${alert.type === "critical" ? "text-red-600" : alert.time === "Today" ? "text-blue-600" : "text-slate-500"}`}>
                    {alert.time}
                  </span>
                  <button className="px-2.5 py-1 text-xs border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-100 transition-opacity font-medium">
                    Action
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Recent Payments + Quick Actions */}
        <div className="space-y-4">
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="px-4 py-3 border-b border-slate-200 bg-slate-50/50 flex justify-between items-center">
              <h3 className="text-sm text-slate-900 flex items-center gap-2">
                <CreditCard className="h-4 w-4 text-blue-600" /> Recent Payments
              </h3>
            </div>
            <div className="divide-y divide-slate-100">
              {RECENT_PAYMENTS.map((pay) => (
                <div key={pay.id} className="px-4 py-3 flex items-center justify-between hover:bg-slate-50 transition-colors">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-slate-800 truncate">{pay.owner}</p>
                    <p className="text-xs text-slate-400">{pay.property} · {pay.date}</p>
                  </div>
                  <div className="text-right flex-shrink-0 ml-3">
                    <p className="text-xs font-bold text-emerald-700">{fmt(pay.amount)}</p>
                    <p className="text-[10px] text-slate-400 mt-0.5">{pay.id}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Quick Actions */}
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="px-4 py-3 border-b border-slate-200 bg-slate-50/50">
              <h3 className="text-sm text-slate-900">Quick Actions</h3>
            </div>
            <div className="p-4 space-y-2">
              {isAuditor ? (
                // Auditor-specific quick actions — read-only, outlined style
                <>
                  <button
                    onClick={() => navigate("/app/audit")}
                    className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-amber-50 transition-colors text-left border border-amber-200 hover:border-amber-300 bg-white"
                  >
                    <div className="p-1.5 rounded-md text-amber-600 bg-amber-50">
                      <ClipboardList className="h-3.5 w-3.5" />
                    </div>
                    <span className="text-sm text-slate-700 font-medium">View Audit Trail</span>
                    <ArrowRight className="h-3.5 w-3.5 text-amber-400 ml-auto" />
                  </button>
                  <button
                    onClick={() => navigate("/app/reporting")}
                    className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-amber-50 transition-colors text-left border border-amber-200 hover:border-amber-300 bg-white"
                  >
                    <div className="p-1.5 rounded-md text-amber-600 bg-amber-50">
                      <FileDown className="h-3.5 w-3.5" />
                    </div>
                    <span className="text-sm text-slate-700 font-medium">Export Report</span>
                    <ArrowRight className="h-3.5 w-3.5 text-amber-400 ml-auto" />
                  </button>
                  <button className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-slate-50 transition-colors text-left border border-slate-100 hover:border-slate-200">
                    <div className="p-1.5 rounded-md text-red-600 bg-red-50">
                      <TrendingDown className="h-3.5 w-3.5" />
                    </div>
                    <span className="text-sm text-slate-700 font-medium">View Delinquency Report</span>
                    <ArrowRight className="h-3.5 w-3.5 text-slate-300 ml-auto" />
                  </button>
                  <button className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-slate-50 transition-colors text-left border border-slate-100 hover:border-slate-200">
                    <div className="p-1.5 rounded-md text-purple-600 bg-purple-50">
                      <Building2 className="h-3.5 w-3.5" />
                    </div>
                    <span className="text-sm text-slate-700 font-medium">Generate Barangay Summary</span>
                    <ArrowRight className="h-3.5 w-3.5 text-slate-300 ml-auto" />
                  </button>
                </>
              ) : (
                // Default quick actions for Admin / Accountant / Staff
                [
                  { label: "Register New Property",     icon: Home,         color: "text-blue-600 bg-blue-50"     },
                  { label: "Record Tax Payment",        icon: CreditCard,   color: "text-emerald-600 bg-emerald-50" },
                  { label: "View Delinquency Report",   icon: TrendingDown, color: "text-red-600 bg-red-50"       },
                  { label: "Generate Barangay Summary", icon: Building2,    color: "text-purple-600 bg-purple-50"  },
                ].map((action) => {
                  const Icon = action.icon;
                  return (
                    <button key={action.label} className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg hover:bg-slate-50 transition-colors text-left border border-slate-100 hover:border-slate-200">
                      <div className={`p-1.5 rounded-md ${action.color}`}>
                        <Icon className="h-3.5 w-3.5" />
                      </div>
                      <span className="text-sm text-slate-700 font-medium">{action.label}</span>
                      <ArrowRight className="h-3.5 w-3.5 text-slate-300 ml-auto" />
                    </button>
                  );
                })
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}