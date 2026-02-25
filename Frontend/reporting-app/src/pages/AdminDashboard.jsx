import React, { useState } from 'react';
import {
    Building2,
    MessageSquareWarning,
    AlertOctagon,
    TrendingUp,
    Clock,
    ChevronRight,
    Plus
} from 'lucide-react';
import { Link } from 'react-router-dom';
import Modal from '../components/Modal';
import './AdminDashboard.css';

// Mock data for initial UI build
const stats = [
    { label: 'Total Organizations', value: '12', icon: Building2, color: 'var(--accent-primary)' },
    { label: 'Active Suggestion Boxes', value: '48', icon: MessageSquareWarning, color: 'var(--success)' },
    { label: 'Active Problem Boxes', value: '24', icon: AlertOctagon, color: 'var(--warning)' },
    { label: 'Total Submissions', value: '1,284', icon: TrendingUp, color: '#a855f7' },
];

const recentActivity = [
    { id: 1, type: 'problem', box: 'IT Support', org: 'Tech Corp', time: '10 mins ago', status: 'new' },
    { id: 2, type: 'suggestion', box: 'Office Improvements', org: 'Design Studio', time: '1 hour ago', status: 'read' },
    { id: 3, type: 'problem', box: 'HR Complaints', org: 'Tech Corp', time: '2 hours ago', status: 'resolved' },
    { id: 4, type: 'suggestion', box: 'Product Ideas', org: 'Startup Inc', time: '3 hours ago', status: 'new' },
];

const AdminDashboard = () => {
    const [isOrgModalOpen, setOrgModalOpen] = useState(false);

    return (
        <div className="dashboard-container animate-fade-in">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Dashboard Overview</h1>
                    <p className="page-description">Welcome back! Here's what's happening across your organizations.</p>
                </div>
                <div className="header-actions">
                    <button className="btn btn-primary" onClick={() => setOrgModalOpen(true)}>
                        <Plus size={18} className="mr-2" /> Create Organization
                    </button>
                </div>
            </div>

            {/* Stats Grid */}
            <div className="stats-grid">
                {stats.map((stat, index) => (
                    <div key={index} className="stat-card glass-panel delay-100">
                        <div className="stat-icon-wrapper" style={{ backgroundColor: `${stat.color}15`, color: stat.color }}>
                            <stat.icon size={24} />
                        </div>
                        <div className="stat-content">
                            <p className="stat-value">{stat.value}</p>
                            <p className="stat-label">{stat.label}</p>
                        </div>
                    </div>
                ))}
            </div>

            <div className="dashboard-content-grid">
                {/* Recent Activity Feed */}
                <div className="dashboard-panel glass-panel delay-200">
                    <div className="panel-header">
                        <h3>Recent Submissions</h3>
                        <Link to="/admin/suggestions" className="view-all-link">View all <ChevronRight size={16} /></Link>
                    </div>
                    <div className="activity-list">
                        {recentActivity.map((activity) => (
                            <div key={activity.id} className="activity-item">
                                <div className={`activity-icon ${activity.type}`}>
                                    {activity.type === 'problem' ? <AlertOctagon size={18} /> : <MessageSquareWarning size={18} />}
                                </div>
                                <div className="activity-details">
                                    <p className="activity-title">
                                        New {activity.type} in <strong>{activity.box}</strong>
                                    </p>
                                    <p className="activity-meta">
                                        {activity.org} &bull; <Clock size={12} className="meta-icon" /> {activity.time}
                                    </p>
                                </div>
                                <div className={`status-dot ${activity.status}`}></div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Quick Actions / Getting Started */}
                <div className="dashboard-panel glass-panel delay-300">
                    <div className="panel-header">
                        <h3>Quick Actions</h3>
                    </div>
                    <div className="quick-actions-grid">
                        <button className="action-btn" onClick={() => setOrgModalOpen(true)}>
                            <div className="action-icon" style={{ color: 'var(--accent-primary)', backgroundColor: 'rgba(99, 102, 241, 0.1)' }}>
                                <Building2 size={24} />
                            </div>
                            <span>Add Organization</span>
                        </button>
                        <button className="action-btn">
                            <div className="action-icon" style={{ color: 'var(--success)', backgroundColor: 'rgba(34, 197, 94, 0.1)' }}>
                                <MessageSquareWarning size={24} />
                            </div>
                            <span>New Suggestion Box</span>
                        </button>
                        <button className="action-btn">
                            <div className="action-icon" style={{ color: 'var(--warning)', backgroundColor: 'rgba(245, 158, 11, 0.1)' }}>
                                <AlertOctagon size={24} />
                            </div>
                            <span>New Problem Box</span>
                        </button>
                    </div>
                </div>
            </div>

            <Modal
                isOpen={isOrgModalOpen}
                onClose={() => setOrgModalOpen(false)}
                title="Create New Organization"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setOrgModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={() => setOrgModalOpen(false)}>Create Organization</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Organization Name</label>
                    <input type="text" className="form-control" placeholder="e.g. Acme Corp" />
                </div>
                <div className="form-group">
                    <label>Primary Contact Email</label>
                    <input type="email" className="form-control" placeholder="admin@acme.com" />
                </div>
                <div className="form-group">
                    <label>Billing Plan</label>
                    <select className="form-control">
                        <option value="Basic">Basic ($49/mo)</option>
                        <option value="Pro">Pro ($199/mo)</option>
                        <option value="Enterprise">Enterprise ($499/mo)</option>
                    </select>
                </div>
            </Modal>
        </div>
    );
};

export default AdminDashboard;
