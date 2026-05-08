import { useEffect, useState } from "react";
import { UserPlus, Search, Edit2, Shield, Trash2, Mail, X, CheckCircle, AlertCircle, Eye, Lock, Users as UsersIcon, ShieldCheck, ShieldOff } from "lucide-react";
import { useAuth, ROLE_META } from "../context/AuthContext";
import type { UserRole } from "../context/AuthContext";
import { AccessDenied } from "../components/RoleGuard";
import { apiJson } from "../lib/apiClient";

type UserStatus = "Active" | "Inactive" | "Pending" | "Suspended";

type ApiUser = {
  userId: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string | null;
  role: string;
  status: string;
  emailVerified: boolean;
  lastLogin?: string | null;
};

type ApiUsersResponse = {
  users: ApiUser[];
  total: number;
  page: number;
  pageSize: number;
};

type SystemUser = {
  id: string; name: string; email: string; role: UserRole;
  status: UserStatus; lastLogin: string; department: string;
};

const CREATABLE_ROLES: UserRole[] = ["Accountant", "Auditor", "Staff"];

const ROLE_DEPARTMENTS: Record<UserRole, string> = {
  Admin: "System Administration",
  Accountant: "Treasury and Finance",
  Staff: "Assessment Division",
  Auditor: "Internal Audit",
};

const toUserRole = (role?: string): UserRole => {
  switch (role) {
    case "Admin":
      return "Admin";
    case "Accountant":
      return "Accountant";
    case "Auditor":
      return "Auditor";
    case "Staff":
      return "Staff";
    case "TaxOfficer":
    case "Taxpayer":
      return "Staff";
    default:
      return "Staff";
  }
};

const toUserStatus = (status?: string): UserStatus => {
  switch (status) {
    case "Active":
      return "Active";
    case "Inactive":
      return "Inactive";
    case "Pending":
      return "Pending";
    case "Suspended":
      return "Suspended";
    default:
      return "Inactive";
  }
};

const formatLastLogin = (value?: string | null) => {
  if (!value) return "Never";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Never";
  return date.toLocaleString();
};

const mapApiUser = (apiUser: ApiUser): SystemUser => {
  const role = toUserRole(apiUser.role);
  const fullName = `${apiUser.firstName ?? ""} ${apiUser.lastName ?? ""}`.trim();
  const name = fullName || apiUser.username || apiUser.email;

  return {
    id: String(apiUser.userId),
    name,
    email: apiUser.email,
    role,
    status: toUserStatus(apiUser.status),
    lastLogin: formatLastLogin(apiUser.lastLogin),
    department: ROLE_DEPARTMENTS[role] ?? "General",
  };
};

const splitFullName = (value: string) => {
  const normalized = value.trim().replace(/\s+/g, " ");
  const parts = normalized.split(" ").filter(Boolean);
  if (parts.length < 2) return null;
  return { firstName: parts[0], lastName: parts.slice(1).join(" ") };
};

const buildUsername = (email: string, fallback: string) => {
  const localPart = email.split("@")[0] ?? "";
  const base = localPart || fallback.replace(/\s+/g, ".");
  const cleaned = base.toLowerCase().replace(/[^a-z0-9._-]/g, "");
  const trimmed = cleaned.slice(0, 50);
  if (trimmed.length >= 3) return trimmed;
  const suffix = Date.now().toString().slice(-6);
  return `user${suffix}`.slice(0, 50);
};

const DEPARTMENTS = ["Treasury & Finance","System Administration","Assessment Division","Internal Audit","External Audit","Management","IT Department","Records Management"];
type ModalMode = "add"|"edit"|"delete"|"view"|null;
const EMPTY_FORM = { name:"", email:"", role:"Staff" as UserRole, status:"Active" as UserStatus, department:"Assessment Division" };
const DEFAULT_PASSWORD_NOTICE = "New accounts use the default initial password configured by the backend.";

const getRoleOptions = (mode: ModalMode, selectedRole?: UserRole): UserRole[] => {
  if (mode === "edit" && selectedRole === "Admin") {
    return ["Admin", ...CREATABLE_ROLES];
  }

  return CREATABLE_ROLES;
};

// Permission summary per role
const PERMISSION_SUMMARY: Record<UserRole, { module: string; access: string }[]> = {
  Admin: [
    { module: "Dashboard",         access: "Full access"              },
    { module: "Property Registry", access: "Full CRUD"                },
    { module: "Tax Calculation",   access: "Full CRUD + rate setting" },
    { module: "Payment Mgmt",      access: "Record, view & edit"      },
    { module: "Compliance",        access: "View & update status"     },
    { module: "Filing & Docs",     access: "Upload, view & delete"    },
    { module: "Govt Reporting",    access: "Generate, approve, export"},
    { module: "Audit Logs",        access: "View & export"            },
    { module: "User Management",   access: "Full CRUD"                },
  ],
  Accountant: [
    { module: "Dashboard",         access: "Full view"                },
    { module: "Property Registry", access: "View, add & edit"         },
    { module: "Tax Calculation",   access: "View & edit"              },
    { module: "Payment Mgmt",      access: "Record, view & edit"      },
    { module: "Compliance",        access: "View & update status"     },
    { module: "Filing & Docs",     access: "Upload, view & delete"    },
    { module: "Govt Reporting",    access: "Generate, submit, export" },
    { module: "Audit Logs",        access: "No access"                },
    { module: "User Management",   access: "No access"                },
  ],
  Staff: [
    { module: "Dashboard",         access: "Full view"                },
    { module: "Property Registry", access: "View & add only"          },
    { module: "Tax Calculation",   access: "View only"                },
    { module: "Payment Mgmt",      access: "Record & view only"       },
    { module: "Compliance",        access: "View only"                },
    { module: "Filing & Docs",     access: "Upload & view (no delete)"},
    { module: "Govt Reporting",    access: "No access"                },
    { module: "Audit Logs",        access: "No access"                },
    { module: "User Management",   access: "No access"                },
  ],
  Auditor: [
    { module: "Dashboard",         access: "View only"                           },
    { module: "Property Registry", access: "View only (read-only · no edits)"    },
    { module: "Tax Calculation",   access: "View only (read-only · no edits)"    },
    { module: "Payment Mgmt",      access: "View only (read-only · no edits)"    },
    { module: "Compliance",        access: "View & monitor (read-only)"           },
    { module: "Filing & Docs",     access: "No access"                           },
    { module: "Govt Reporting",    access: "View & export only (no generate)"    },
    { module: "Audit Logs",        access: "Full view · export · no modifications"},
    { module: "User Management",   access: "No access"                           },
  ],
};

export default function Users() {
  const { can } = useAuth();

  // Admin-only module
  if (!can("users.view")) {
    return <AccessDenied requiredRole="Admin" />;
  }

  const [users,      setUsers]     = useState<SystemUser[]>([]);
  const [loading,    setLoading]   = useState(false);
  const [modal,      setModal]     = useState<ModalMode>(null);
  const [selected,   setSelected]  = useState<SystemUser | null>(null);
  const [form,       setForm]      = useState(EMPTY_FORM);
  const [search,     setSearch]    = useState("");
  const [roleFilter, setRoleFilter]= useState("All Roles");
  const [statFilter, setStatFilter]= useState("All");
  const [toast,      setToast]     = useState<{ msg: string; type: "success"|"error" } | null>(null);
  const [activeRoleTab, setActiveRoleTab] = useState<UserRole>("Admin");

  const showToast = (msg: string, type: "success"|"error" = "success") => {
    setToast({ msg, type }); setTimeout(() => setToast(null), 3000);
  };

  const loadUsers = async () => {
    setLoading(true);
    try {
      const data = await apiJson<ApiUsersResponse>("/users");
      setUsers(data.users.map(mapApiUser));
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unable to load users.";
      showToast(message, "error");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadUsers();
  }, []);

  const filteredUsers = users.filter((u) => {
    const mS = u.name.toLowerCase().includes(search.toLowerCase()) || u.email.toLowerCase().includes(search.toLowerCase()) || u.role.toLowerCase().includes(search.toLowerCase());
    const mR = roleFilter === "All Roles" || u.role === roleFilter;
    const mSt = statFilter === "All" || u.status === statFilter;
    return mS && mR && mSt;
  });

  const openAdd  = () => { setForm(EMPTY_FORM); setSelected(null); setModal("add"); };
  const openEdit = (u: SystemUser) => { setSelected(u); setForm({ name:u.name, email:u.email, role:u.role, status:u.status, department:u.department }); setModal("edit"); };
  const openView = (u: SystemUser) => { setSelected(u); setModal("view"); };
  const openDel  = (u: SystemUser) => { setSelected(u); setModal("delete"); };

  const updateUserStatus = async (userId: string, status: UserStatus, silent = false) => {
    await apiJson<{ message: string }>(`/users/${userId}/status`, {
      method: "PUT",
      body: JSON.stringify({ status }),
    });

    setUsers((p) => p.map((u) => (u.id === userId ? { ...u, status } : u)));
    if (!silent) {
      showToast(`User status set to ${status}.`);
    }
  };

  const handleSave = async () => {
    if (!form.name.trim() || !form.email.trim()) {
      showToast("Fill in all required fields.", "error");
      return;
    }

    const nameParts = splitFullName(form.name);
    if (!nameParts) {
      showToast("Please enter both first and last name.", "error");
      return;
    }

    try {
      if (modal === "add") {
        const payload = {
          username: buildUsername(form.email, nameParts.firstName),
          email: form.email.trim(),
          firstName: nameParts.firstName,
          lastName: nameParts.lastName,
          role: form.role,
        };

        const created = await apiJson<ApiUser>("/users", {
          method: "POST",
          body: JSON.stringify(payload),
        });

        const createdUser = {
          ...mapApiUser(created),
          status: form.status,
          department: form.department,
          lastLogin: "Never",
        };

        setUsers((p) => [createdUser, ...p]);

        if (form.status !== "Active") {
          await updateUserStatus(createdUser.id, form.status, true);
        }

        showToast(`User "${createdUser.name}" created with the default initial password.`);
      } else if (modal === "edit" && selected) {
        const payload = {
          email: form.email.trim(),
          firstName: nameParts.firstName,
          lastName: nameParts.lastName,
          role: form.role,
          status: form.status,
        };

        const updated = await apiJson<ApiUser>(`/users/${selected.id}`, {
          method: "PUT",
          body: JSON.stringify(payload),
        });

        const updatedUser = {
          ...mapApiUser(updated),
          status: form.status,
          department: form.department,
        };

        setUsers((p) => p.map((u) => (u.id === selected.id ? updatedUser : u)));
        showToast(`User "${updatedUser.name}" updated.`);
      }

      setModal(null);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unable to save user.";
      showToast(message, "error");
    }
  };

  const handleDelete = async () => {
    if (!selected) return;

    try {
      await apiJson<{ message: string }>(`/users/${selected.id}`, {
        method: "DELETE",
      });

      setUsers((p) => p.filter((u) => u.id !== selected.id));
      showToast("User removed from system.");
      setModal(null);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unable to delete user.";
      showToast(message, "error");
    }
  };

  const toggleStatus = async (u: SystemUser) => {
    const nextStatus: UserStatus = u.status === "Active" ? "Inactive" : "Active";

    try {
      await updateUserStatus(u.id, nextStatus);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unable to update status.";
      showToast(message, "error");
    }
  };

  const counts = { total: users.length, active: users.filter(u=>u.status==="Active").length, admin: users.filter(u=>u.role==="Admin").length, inactive: users.filter(u=>u.status==="Inactive").length };

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Toast */}
      {toast && (
        <div className={`fixed top-5 right-5 z-50 flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg border text-sm font-medium ${toast.type==="success" ? "bg-emerald-50 border-emerald-200 text-emerald-800" : "bg-red-50 border-red-200 text-red-800"}`}>
          {toast.type==="success" ? <CheckCircle className="h-4 w-4 text-emerald-500"/> : <AlertCircle className="h-4 w-4 text-red-500"/>}
          {toast.msg}
        </div>
      )}

      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-slate-900 tracking-tight">User Management</h1>
          <p className="text-sm text-slate-500 mt-1">Manage Davao Region LGU system accounts, roles, and security permissions. Admin access only.</p>
        </div>
        <button onClick={openAdd} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors shadow-sm flex items-center gap-2">
          <UserPlus className="h-4 w-4" /> Add New User
        </button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label:"Total Users",    value: counts.total,    icon: UsersIcon,   color: "border-l-blue-500",    bg: "bg-blue-50 text-blue-600"    },
          { label:"Active",         value: counts.active,   icon: ShieldCheck, color: "border-l-emerald-500", bg: "bg-emerald-50 text-emerald-600"},
          { label:"Inactive",       value: counts.inactive, icon: ShieldOff,   color: "border-l-slate-400",   bg: "bg-slate-100 text-slate-600"  },
          { label:"Admin Accounts", value: counts.admin,    icon: Shield,      color: "border-l-purple-500",  bg: "bg-purple-50 text-purple-600" },
        ].map(({ label, value, icon: Icon, color, bg }) => (
          <div key={label} className={`bg-white p-4 rounded-xl border border-slate-200 border-l-4 shadow-sm ${color}`}>
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-start gap-3 flex-1 min-w-0">
                <div className={`p-2 rounded-lg flex-shrink-0 ${bg}`}><Icon className="h-4 w-4" /></div>
                <div className="min-w-0">
                  <p className="text-xs text-slate-500">{label}</p>
                </div>
              </div>
              <div className="text-right flex-shrink-0">
                <p className="text-3xl font-bold text-slate-900">{value}</p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* User Table */}
      <div className="bg-white border border-slate-200 shadow-sm rounded-xl overflow-hidden">
        <div className="p-4 border-b border-slate-200 bg-slate-50/50 flex flex-wrap gap-3 items-center justify-between">
          <div className="relative flex-1 min-w-48 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
            <input type="text" placeholder="Search by name, email, or role..." value={search} onChange={(e)=>setSearch(e.target.value)}
              className="pl-8 pr-3 py-2 w-full border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white"/>
          </div>
          <div className="flex gap-2">
            <select value={roleFilter} onChange={(e)=>setRoleFilter(e.target.value)}
              className="px-3 py-2 border border-slate-200 rounded-lg text-sm bg-white text-slate-600 focus:outline-none">
              <option>All Roles</option>
              {(["Admin","Accountant","Staff","Auditor"] as UserRole[]).map((r)=><option key={r}>{r}</option>)}
            </select>
            <select value={statFilter} onChange={(e)=>setStatFilter(e.target.value)}
              className="px-3 py-2 border border-slate-200 rounded-lg text-sm bg-white text-slate-600 focus:outline-none">
              <option>All</option><option>Active</option><option>Inactive</option><option>Pending</option><option>Suspended</option>
            </select>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm whitespace-nowrap">
            <thead className="bg-slate-50 text-slate-500 text-xs uppercase font-medium border-b border-slate-200 tracking-wider">
              <tr>
                <th className="px-5 py-3.5">User</th>
                <th className="px-5 py-3.5">Role</th>
                <th className="px-5 py-3.5">Department</th>
                <th className="px-5 py-3.5">Status</th>
                <th className="px-5 py-3.5">Last Active</th>
                <th className="px-5 py-3.5 text-center">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {loading ? (
                <tr><td colSpan={6} className="px-5 py-10 text-center text-slate-400 text-sm">Loading users...</td></tr>
              ) : filteredUsers.length === 0 ? (
                <tr><td colSpan={6} className="px-5 py-10 text-center text-slate-400 text-sm">No users found.</td></tr>
              ) : (
                filteredUsers.map((u) => {
                  const meta = ROLE_META[u.role];
                  return (
                    <tr key={u.id} className="hover:bg-slate-50 transition-colors group">
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3">
                          <div className={`h-9 w-9 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0 ${meta.badgeClass}`}>
                            {u.name.split(" ").map(n=>n[0]).join("").slice(0,2)}
                          </div>
                          <div>
                            <p className="font-medium text-slate-900">{u.name}</p>
                            <p className="text-xs text-slate-500 flex items-center gap-1 mt-0.5"><Mail className="h-3 w-3" />{u.email}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-5 py-4">
                        <div className={`inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-bold border ${meta.badgeClass}`}>
                          <Shield className="h-3 w-3" /> {meta.label}
                          <span className={`ml-1 text-[9px] px-1 py-0.5 rounded-full bg-current bg-opacity-10`}>L{meta.accessLevel}</span>
                        </div>
                      </td>
                      <td className="px-5 py-4 text-xs text-slate-600">{u.department}</td>
                      <td className="px-5 py-4">
                        <button onClick={() => toggleStatus(u)}
                          className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold hover:opacity-80 transition-opacity ${u.status==="Active" ? "bg-emerald-100 text-emerald-700" : "bg-slate-100 text-slate-600"}`}>
                          <span className={`h-1.5 w-1.5 rounded-full ${u.status==="Active" ? "bg-emerald-500" : "bg-slate-400"}`} />
                          {u.status}
                        </button>
                      </td>
                      <td className="px-5 py-4 text-xs text-slate-500">{u.lastLogin}</td>
                      <td className="px-5 py-4">
                        <div className="flex justify-center gap-1.5">
                          <button onClick={()=>openView(u)} className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg" title="View"><Eye className="h-3.5 w-3.5"/></button>
                          <button onClick={()=>openEdit(u)} className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg border border-transparent hover:border-blue-200" title="Edit"><Edit2 className="h-3.5 w-3.5"/></button>
                          <button onClick={()=>openDel(u)}  className="p-1.5 text-slate-400 hover:text-red-600  hover:bg-red-50  rounded-lg border border-transparent hover:border-red-200"  title="Delete"><Trash2 className="h-3.5 w-3.5"/></button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Role Permission Matrix */}
      <div className="bg-white border border-slate-200 shadow-sm rounded-xl overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 bg-slate-50 flex items-center gap-2">
          <Shield className="h-4 w-4 text-blue-600" />
          <h3 className="text-slate-900">Role Access Matrix</h3>
          <span className="ml-auto text-xs text-slate-400">Click a role to view permissions</span>
        </div>
        {/* Role tabs */}
        <div className="flex border-b border-slate-100 bg-slate-50/50">
          {(["Admin","Accountant","Staff","Auditor"] as UserRole[]).map((role) => {
            const meta = ROLE_META[role];
            return (
              <button key={role} onClick={() => setActiveRoleTab(role)}
                className={`flex-1 flex flex-col items-center gap-1 py-3 px-2 text-xs font-medium border-b-2 transition-colors ${activeRoleTab === role ? `border-current ${meta.textClass}` : "border-transparent text-slate-500 hover:text-slate-700"}`}>
                <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${meta.badgeClass}`}>{meta.label}</span>
                <span className="text-[10px] text-slate-400">Level {meta.accessLevel}</span>
                <span className="text-[10px] font-medium">{users.filter(u=>u.role===role).length} user{users.filter(u=>u.role===role).length!==1?"s":""}</span>
              </button>
            );
          })}
        </div>
        {/* Permission details */}
        <div className="p-5">
          <div className="flex items-center gap-3 mb-4">
            <div className={`p-2.5 rounded-xl ${ROLE_META[activeRoleTab].badgeClass}`}>
              <Shield className="h-5 w-5" />
            </div>
            <div>
              <p className="font-semibold text-slate-900">{ROLE_META[activeRoleTab].label}</p>
              <p className="text-xs text-slate-500 mt-0.5">{ROLE_META[activeRoleTab].description}</p>
            </div>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {PERMISSION_SUMMARY[activeRoleTab].map(({ module, access }) => {
              const isNoAccess = access === "No access";
              return (
                <div key={module} className={`flex items-center justify-between px-3 py-2.5 rounded-lg border text-xs ${isNoAccess ? "bg-slate-50 border-slate-100" : `${ROLE_META[activeRoleTab].bgClass} border-opacity-40`}`}
                  style={{ borderColor: isNoAccess ? "" : "currentColor" }}>
                  <span className={`font-medium ${isNoAccess ? "text-slate-400" : "text-slate-700"}`}>{module}</span>
                  <span className={`px-2 py-0.5 rounded-full font-medium ${isNoAccess ? "bg-slate-200 text-slate-400" : `${ROLE_META[activeRoleTab].badgeClass}`}`}>
                    {isNoAccess ? <Lock className="h-3 w-3 inline"/> : null} {access}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Add/Edit Modal */}
      {(modal==="add"||modal==="edit") && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200 bg-slate-50">
              <h3 className="text-slate-900">{modal==="add" ? "Add New User" : "Edit User"}</h3>
              <button onClick={()=>setModal(null)} className="p-1.5 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg"><X className="h-4 w-4"/></button>
            </div>
            <div className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="col-span-2">
                  <label className="block text-sm text-slate-700 mb-1.5">Full Name <span className="text-red-500">*</span></label>
                  <input type="text" placeholder="Enter full name" value={form.name} onChange={(e)=>setForm(f=>({...f,name:e.target.value}))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500"/>
                </div>
                <div className="col-span-2">
                  <label className="block text-sm text-slate-700 mb-1.5">Email Address <span className="text-red-500">*</span></label>
                  <input type="email" placeholder="user@taxsync.gov.ph" value={form.email} onChange={(e)=>setForm(f=>({...f,email:e.target.value}))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500"/>
                </div>
                <div>
                  <label className="block text-sm text-slate-700 mb-1.5">Role <span className="text-red-500">*</span></label>
                  <select value={form.role} onChange={(e)=>setForm(f=>({...f,role:e.target.value as UserRole}))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white">
                    {getRoleOptions(modal, selected?.role).map((r)=><option key={r}>{r}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-sm text-slate-700 mb-1.5">Status</label>
                  <select value={form.status} onChange={(e)=>setForm(f=>({...f,status:e.target.value as UserStatus}))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white">
                    <option>Active</option><option>Inactive</option><option>Pending</option><option>Suspended</option>
                  </select>
                </div>
                <div className="col-span-2">
                  <label className="block text-sm text-slate-700 mb-1.5">Department</label>
                  <select value={form.department} onChange={(e)=>setForm(f=>({...f,department:e.target.value}))}
                    className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white">
                    {DEPARTMENTS.map((d)=><option key={d}>{d}</option>)}
                  </select>
                </div>
                {modal==="add" && (
                  <div className="col-span-2">
                    <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                      <p className="font-medium"><Lock className="h-3.5 w-3.5 inline mr-1" /> Initial Password Policy</p>
                      <p className="mt-1 text-amber-800">{DEFAULT_PASSWORD_NOTICE}</p>
                      <p className="mt-1 text-xs text-amber-700">Users can change it later using the Change Password screen.</p>
                    </div>
                  </div>
                )}
              </div>
              {/* Role permission preview */}
              <div className={`p-3 rounded-lg border ${ROLE_META[form.role].bgClass} border-opacity-50`}>
                <p className={`text-xs font-bold mb-2 ${ROLE_META[form.role].textClass}`}>
                  <Shield className="h-3 w-3 inline mr-1" /> {ROLE_META[form.role].label} Permissions Preview:
                </p>
                <div className="grid grid-cols-2 gap-1">
                  {PERMISSION_SUMMARY[form.role].slice(0,4).map(({module,access})=>(
                    <div key={module} className="text-xs flex items-center gap-1">
                      {access==="No access" ? <Lock className="h-2.5 w-2.5 text-slate-400"/> : <span className="w-2.5 h-2.5 rounded-full bg-current flex-shrink-0 opacity-60" style={{}} />}
                      <span className={access==="No access" ? "text-slate-400" : ROLE_META[form.role].textClass}>{module}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
            <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-slate-200 bg-slate-50">
              <button onClick={()=>setModal(null)} className="px-4 py-2 border border-slate-200 rounded-lg text-sm text-slate-600 hover:bg-slate-100">Cancel</button>
              <button onClick={handleSave} className="px-5 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 shadow-sm">
                {modal==="add" ? "Create User" : "Save Changes"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* View Modal */}
      {modal==="view" && selected && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden">
            <div className={`p-6 text-center border-b border-slate-100 ${ROLE_META[selected.role].bgClass}`}>
              <div className={`w-16 h-16 rounded-full flex items-center justify-center text-xl font-bold mx-auto mb-3 ${ROLE_META[selected.role].badgeClass}`}>
                {selected.name.split(" ").map(n=>n[0]).join("").slice(0,2)}
              </div>
              <h3 className="text-slate-900">{selected.name}</h3>
              <p className="text-sm text-slate-500 mt-0.5">{selected.email}</p>
              <span className={`inline-flex items-center gap-1.5 mt-2 px-3 py-1 rounded-full text-xs font-bold border ${ROLE_META[selected.role].badgeClass}`}>
                <Shield className="h-3 w-3" /> {ROLE_META[selected.role].label} · Level {ROLE_META[selected.role].accessLevel}
              </span>
            </div>
            <div className="p-5 space-y-2">
              {[["User ID",selected.id],["Department",selected.department],["Status",selected.status],["Last Login",selected.lastLogin]].map(([l,v])=>(
                <div key={l} className="flex justify-between py-2 border-b border-slate-100">
                  <span className="text-xs text-slate-500">{l}</span>
                  <span className="text-sm font-medium text-slate-800">{v}</span>
                </div>
              ))}
              <div className="pt-3">
                <p className="text-xs text-slate-500 mb-2 font-medium">Module Access:</p>
                <div className="grid grid-cols-1 gap-1.5">
                  {PERMISSION_SUMMARY[selected.role].map(({module,access})=>(
                    <div key={module} className={`flex items-center justify-between px-2.5 py-1.5 rounded-lg text-xs ${access==="No access" ? "bg-slate-50" : ROLE_META[selected.role].bgClass}`}>
                      <span className={access==="No access" ? "text-slate-400" : "text-slate-700 font-medium"}>{module}</span>
                      <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium ${access==="No access" ? "bg-slate-200 text-slate-400" : ROLE_META[selected.role].badgeClass}`}>
                        {access==="No access" ? "🔒 No access" : access}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
            <div className="flex gap-3 px-6 pb-6">
              <button onClick={()=>setModal(null)} className="flex-1 py-2 border border-slate-200 rounded-lg text-sm text-slate-600 hover:bg-slate-50">Close</button>
              <button onClick={()=>openEdit(selected)} className="flex-1 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700">Edit User</button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Modal */}
      {modal==="delete" && selected && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm overflow-hidden">
            <div className="p-6 text-center">
              <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4"><Trash2 className="h-6 w-6 text-red-600"/></div>
              <h3 className="text-slate-900 mb-2">Remove User?</h3>
              <p className="text-sm text-slate-500 mb-3">This will permanently remove <span className="font-medium text-slate-900">{selected.name}</span> and revoke all access.</p>
              <p className="text-xs text-red-500">This action cannot be undone.</p>
            </div>
            <div className="flex gap-3 px-6 pb-6">
              <button onClick={()=>setModal(null)} className="flex-1 px-4 py-2 border border-slate-200 rounded-lg text-sm text-slate-600 hover:bg-slate-50">Cancel</button>
              <button onClick={handleDelete} className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700">Remove User</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}