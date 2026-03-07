import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import {
    LayoutDashboard,
    Building2,
    MessageSquareWarning,
    AlertOctagon,
    Users,
    CreditCard,
    Settings,
    LogOut,
    ShieldAlert
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import './Sidebar.css';

const Sidebar = () => {
    const { user, logout } = useAuth();
    const navigate = useNavigate();

    // Use the role name directly from AuthContext
    const role = user?.role || 'user';

    // Navigation items based on role (Admin, Manager, Billing)
    const navItems = [
        { name: 'Dashboard', path: '/admin/dashboard', icon: LayoutDashboard, roles: ['admin'] },
        { name: 'Suggestion Boxes', path: '/admin/suggestions', icon: MessageSquareWarning, roles: ['admin', 'manager'] },
        { name: 'Problem Boxes', path: '/admin/problems', icon: AlertOctagon, roles: ['admin', 'manager'] },
        { name: 'Users', path: '/admin/users', icon: Users, roles: ['admin'] },
        { name: 'Billing', path: '/admin/billing', icon: CreditCard, roles: ['billingmanager'] },
    ];

    const visibleNavItems = navItems.filter(item => item.roles.includes(role));

    return (
        <aside className="sidebar glass-panel">
            <div className="sidebar-header">
                <div className="logo-icon-wrapper-small">
                    <ShieldAlert size={20} className="logo-icon" />
                </div>
                <span className="logo-text">SecureReport</span>
            </div>

            <div className="sidebar-user-profile">
                <div className="avatar">
                    {user?.name?.charAt(0).toUpperCase() || 'A'}
                </div>
                <div className="user-info">
                    <p className="user-name">{user?.name || 'User'}</p>
                    <p className="user-role">{role.toUpperCase()}</p>
                </div>
            </div>

            <nav className="sidebar-nav">
                <ul>
                    {visibleNavItems.map((item) => (
                        <li key={item.name}>
                            <NavLink
                                to={item.path}
                                className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
                            >
                                <item.icon size={20} className="nav-icon" />
                                <span>{item.name}</span>
                            </NavLink>
                        </li>
                    ))}
                </ul>
            </nav>

            <div className="sidebar-footer">
                <NavLink to="/admin/settings" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
                    <Settings size={20} className="nav-icon" />
                    <span>Settings</span>
                </NavLink>
                <button className="nav-item logout-btn" onClick={() => {
                    logout();
                    navigate('/login');
                }}>
                    <LogOut size={20} className="nav-icon" />
                    <span>Sign Out</span>
                </button>
            </div>
        </aside>
    );
};

export default Sidebar;
