import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, Outlet } from 'react-router-dom';
import Navbar from './components/Navbar';
import Home from './pages/Home';
import Login from './pages/Login';
import PublicPortal from './pages/PublicPortal';
import AdminLayout from './components/Layout/AdminLayout';
import AdminDashboard from './pages/AdminDashboard';
import OrganizationDetails from './pages/OrganizationDetails';
import BoxDetails from './pages/BoxDetails';
import SubmissionDetails from './pages/SubmissionDetails';
import BillingDashboard from './pages/BillingDashboard';
import TrackReport from './pages/TrackReport';
import AnonymousSignup from './pages/AnonymousSignup';
import AnonymousLogin from './pages/AnonymousLogin';
import AnonymousSubmit from './pages/AnonymousSubmit';
import UserManagement from './pages/UserManagement';
import BoxSettings from './pages/BoxSettings';
import SubscriptionDetails from './pages/SubscriptionDetails';
import ProblemBoxes from './pages/ProblemBoxes';
import SuggestionBoxes from './pages/SuggestionBoxes';
import Settings from './pages/Settings';
import CommunityDashboard from './pages/CommunityDashboard';
import { AuthProvider, useAuth } from './context/AuthContext';
import './index.css';

// Simple Protected Route wrapper
const ProtectedRoute = ({ children, allowedRoles }) => {
  const { user } = useAuth();

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/admin/dashboard" replace />;
  }

  return children;
};

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="app-container">
          <Routes>
            {/* Public Routes (with Navbar wrapper) */}
            <Route element={
              <>
                <Navbar />
                <main className="main-content">
                  <Outlet />
                </main>
              </>
            }>
              <Route path="/" element={<Home />} />
              <Route path="/login" element={<Login />} />
              <Route path="/signup" element={<Login />} />
              <Route path="/portal" element={<PublicPortal />} />
              <Route path="/track" element={<TrackReport />} />
              <Route path="/anonymous/signup" element={<AnonymousSignup />} />
              <Route path="/anonymous/login" element={<AnonymousLogin />} />
              <Route path="/anonymous/submit" element={<AnonymousSubmit />} />
              <Route path="/organizations" element={<Navigate to="/portal" replace />} />
            </Route>

            {/* Authenticated Admin/Manager Routes */}
            <Route path="/admin" element={
              <ProtectedRoute>
                <AdminLayout />
              </ProtectedRoute>
            }>
              <Route index element={<Navigate to="/admin/dashboard" replace />} />
              <Route path="dashboard" element={<AdminDashboard />} />
              <Route path="organizations" element={<Navigate to="/admin/organizations/ORG-001" replace />} />
              <Route path="organizations/:orgId" element={<OrganizationDetails />} />
              <Route path="boxes/:boxId" element={<BoxDetails />} />
              <Route path="boxes/:boxId/settings" element={<BoxSettings />} />
              <Route path="submissions/:submissionId" element={<SubmissionDetails />} />
              <Route path="billing" element={<BillingDashboard />} />
              <Route path="billing/:orgId" element={<SubscriptionDetails />} />
              {/* Placeholders for future pages */}
              <Route path="community" element={<CommunityDashboard />} />
              <Route path="suggestions" element={<SuggestionBoxes />} />
              <Route path="problems" element={<ProblemBoxes />} />
              <Route path="users" element={<ProtectedRoute allowedRoles={['admin']}><UserManagement /></ProtectedRoute>} />
              <Route path="settings" element={<Settings />} />
            </Route>
          </Routes>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
