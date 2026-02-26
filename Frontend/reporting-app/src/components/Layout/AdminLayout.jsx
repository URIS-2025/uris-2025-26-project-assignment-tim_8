import React from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import './AdminLayout.css';

const AdminLayout = () => {
    return (
        <div className="admin-layout">
            <Sidebar />
            <div className="admin-main-wrapper">
                <header className="admin-header glass-panel">
                    <div className="header-breadcrumbs">
                        <span className="text-gradient">SecureReport Workspace</span>
                    </div>
                    <div className="header-actions">
                        {/* Additional header actions like notifications can go here */}
                    </div>
                </header>
                <main className="admin-content">
                    <Outlet /> {/* Renders the nested routes (e.g. AdminDashboard) */}
                </main>
            </div>

            {/* Background ambient light specific to admin section */}
            <div className="admin-ambient-light"></div>
        </div>
    );
};

export default AdminLayout;
