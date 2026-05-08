import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './components/AppLayout'
import { AuthProvider } from './context/AuthContext'
import LandingPage from './pages/landing/LandingPage'
import Login from './Login'
import Audit from './pages/Audit'
import Compliance from './pages/Compliance'
import Dashboard from './pages/Dashboard'
import Filing from './pages/Filing'
import PaymentManagement from './pages/PaymentManagement'
import PropertyRegistration from './pages/PropertyRegistration'
import Reporting from './pages/Reporting'
import TaxCalculation from './pages/TaxCalculation'
import Users from './pages/Users'
import ResetPassword from './pages/ResetPassword'
import VerifyEmail from './pages/VerifyEmail'

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<Login />} />
        <Route path="/reset-password" element={<ResetPassword />} />
        <Route path="/verify-email" element={<VerifyEmail />} />
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<Dashboard />} />
          <Route path="property-registration" element={<PropertyRegistration />} />
          <Route path="tax-calculation" element={<TaxCalculation />} />
          <Route path="payment-management" element={<PaymentManagement />} />
          <Route path="compliance" element={<Compliance />} />
          <Route path="filing" element={<Filing />} />
          <Route path="reporting" element={<Reporting />} />
          <Route path="audit" element={<Audit />} />
          <Route path="users" element={<Users />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  )
}

export default App
