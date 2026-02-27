import React, { useState, useEffect } from 'react';
import {
    Building2,
    MessageSquareWarning,
    AlertOctagon,
    TrendingUp,
    Clock,
    ChevronRight,
    Plus
} from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import Modal from '../components/Modal';
import { OrganizationService } from '../services/organizationService';
import './AdminDashboard.css';
import { SuggestionBoxService } from '../services/suggestionBoxService';

const AdminDashboard = () => {
    const [isOrgModalOpen, setOrgModalOpen] = useState(false);
    const [organizations, setOrganizations] = useState([]);
    const [suggestionBoxes, setSuggestionBoxes] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    // Form state for creating organization
    const [newOrg, setNewOrg] = useState({ name: '', themeColor: '#000000', customLogoUrl: '' });

    const navigate = useNavigate();

    useEffect(() => {
        fetchOrganizations();
        fetchSuggestionBoxes();
    }, []);

    const fetchOrganizations = async () => {
        try {
            setIsLoading(true);
            const data = await OrganizationService.getAll();
            setOrganizations(data);
        } catch (error) {
            console.error("Failed to fetch organizations", error);
        } finally {
            setIsLoading(false);
        }
    };

    const fetchSuggestionBoxes = async () => {
        try {
            setIsLoading(true);
            const data = await SuggestionBoxService.getAll();
            setSuggestionBoxes(data);
        } catch (error) {
            console.error("Failed to fetch suggestion boxes", error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleCreateOrganization = async () => {
        if (!newOrg.name.trim()) return;
        try {
            const created = await OrganizationService.create({
                name: newOrg.name,
                themeColor: newOrg.themeColor || '#6366f1',
                customLogoUrl: newOrg.customLogoUrl || null,
            });
            setOrgModalOpen(false);
            setNewOrg({ name: '', themeColor: '#000000', customLogoUrl: '' });
            fetchOrganizations();
            navigate(`/admin/organizations/${created.id}`);
        } catch (error) {
            console.error("Failed to create organization", error);
            alert("Failed to create organization");
        }
    };

    const displayStats = [
        { label: 'Total Organizations', value: isLoading ? '...' : organizations.length.toString(), icon: Building2, color: 'var(--accent-primary)' },
        { label: 'Active Suggestion Boxes', value: isLoading ? '...' : suggestionBoxes.length.toString(), icon: MessageSquareWarning, color: 'var(--success)' },
        { label: 'Active Problem Boxes', value: '24', icon: AlertOctagon, color: 'var(--warning)' },
        { label: 'Total Submissions', value: '1,284', icon: TrendingUp, color: '#a855f7' },
    ];

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
                {displayStats.map((stat, index) => (
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
                {/* Organizations List */}
                <div className="dashboard-panel glass-panel delay-200">
                    <div className="panel-header">
                        <h3>Organizations</h3>
                    </div>
                    <div className="activity-list">
                        {isLoading ? (
                            <p className="p-4 text-center">Loading...</p>
                        ) : organizations.length === 0 ? (
                            <p className="p-4 text-center">No organizations found.</p>
                        ) : (
                            organizations.slice(0, 5).map((org) => (
                                <Link to={`/admin/organizations/${org.id}`} key={org.id} className="activity-item" style={{ textDecoration: 'none', color: 'inherit' }}>
                                    <div className="activity-icon suggestion">
                                        <Building2 size={18} />
                                    </div>
                                    <div className="activity-details">
                                        <p className="activity-title">
                                            <strong>{org.name}</strong>
                                        </p>
                                        <p className="activity-meta">
                                            Organization ID: {org.id}
                                        </p>
                                    </div>
                                    <ChevronRight size={16} style={{ opacity: 0.5 }} />
                                </Link>
                            ))
                        )}
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
                        <button className="btn btn-primary" onClick={handleCreateOrganization}>Create Organization</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Organization Name *</label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="e.g. Acme Corp"
                        value={newOrg.name}
                        onChange={(e) => setNewOrg({ ...newOrg, name: e.target.value })}
                        required
                    />
                </div>
                <div className="form-group">
                    <label>Theme Color</label>
                    <div className="flex items-center gap-3">
                        <input
                            type="color"
                            className="form-control"
                            style={{ width: '60px', padding: '0 4px', height: '40px' }}
                            value={newOrg.themeColor}
                            onChange={(e) => setNewOrg({ ...newOrg, themeColor: e.target.value })}
                        />
                        <span className="text-sm opacity-70">{newOrg.themeColor}</span>
                    </div>
                </div>
                <div className="form-group">
                    <label>Logo URL (Optional)</label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="https://..."
                        value={newOrg.customLogoUrl}
                        onChange={(e) => setNewOrg({ ...newOrg, customLogoUrl: e.target.value })}
                    />
                </div>
            </Modal>
        </div>
    );
};

export default AdminDashboard;
