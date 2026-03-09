import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { Building2, Users, MessageSquareWarning, AlertOctagon, ArrowLeft, Plus, Settings, Bell, X, Trash2 } from 'lucide-react';
import { OrganizationService } from '../services/organizationService';
import { SuggestionBoxService } from '../services/suggestionBoxService';
import { ProblemBoxService } from '../services/problemBoxService';
import { UserService } from '../services/userService';
import { BillingNotificationService } from '../services/billingNotificationService';
import { SystemNotificationService } from '../services/systemNotificationService';
import './OrganizationDetails.css';
import { useAuth } from '../context/AuthContext';

const mockManagers = [
    { id: 'M-101', name: 'Alice Walker', email: 'alice@techcorp.com', role: 'Head Manager', added: 'Feb 01, 2023' },
    { id: 'M-102', name: 'Bob Smith', email: 'bob@techcorp.com', role: 'Manager', added: 'Mar 15, 2023' },
];

const OrganizationDetails = () => {
    const { orgId } = useParams();
    const { user } = useAuth();
    const navigate = useNavigate();

    const [organization, setOrganization] = useState(null);
    const [isLoading, setIsLoading] = useState(true);

    // Modal states
    const [suggestionBoxes, setSuggestionBoxes] = useState([]);
    const [problemBoxes, setProblemBoxes] = useState([]);
    const [isManagerModalOpen, setManagerModalOpen] = useState(false);
    const [isBoxModalOpen, setBoxModalOpen] = useState(false);
    const [isSettingsModalOpen, setSettingsModalOpen] = useState(false);
    const [editOrg, setEditOrg] = useState({ name: '', themeColor: '', customLogoUrl: '' });
    const [activeTab, setActiveTab] = useState('boxes');
    const [managers, setManagers] = useState([]);

    const [newBox, setNewBox] = useState({
        name: '',
        type: 'suggestion',
        description: '',
        password: '',
        isDarkTheme: false,
        createdBy: ''
    });

    const [boxSearch, setBoxSearch] = useState('');
    const [managerSearch, setManagerSearch] = useState('');

    // Notification bell state
    const [notifications, setNotifications] = useState([]);
    const [showNotifications, setShowNotifications] = useState(false);

    const [newManager, setNewManager] = useState({
        id: ''
    });

    useEffect(() => {
        fetchSuggestionBoxes();
        fetchProblemBoxes();
        fetchOrganization();
        fetchManagers();
        fetchNotifications();
    }, [orgId]);

    const fetchNotifications = async () => {
        try {
            const billingData = await BillingNotificationService.getAll();
            if (Array.isArray(billingData)) {
                const filtered = billingData.filter(n => n.organizationId === orgId);
                setNotifications(filtered);
            } else {
                setNotifications([]);
            }
        } catch (err) {
            console.error('Failed to fetch notifications:', err);
            setNotifications([]);
        }
    };

    const handleDeleteNotification = async (notifId) => {
        try {
            await BillingNotificationService.delete(notifId);
            setNotifications(prev => prev.filter(n => n.id !== notifId));
        } catch (err) {
            console.error('Failed to delete notification:', err);
        }
    };

    const fetchOrganization = async () => {
        try {
            setIsLoading(true);
            const data = await OrganizationService.getById(orgId);
            setOrganization(data);
            setEditOrg({
                name: data.name,
                themeColor: data.themeColor || '#6366f1',
                customLogoUrl: data.customLogoUrl || ''
            });
        } catch (error) {
            console.error("Failed to fetch organization", error);
        } finally {
            setIsLoading(false);
        }
    };

    const fetchSuggestionBoxes = async () => {
        try {
            setIsLoading(true);
            const data = await SuggestionBoxService.getByOrganizationId(orgId);
            setSuggestionBoxes(data);
        } catch (error) {
            console.error("Failed to fetch suggestion boxes", error);
        } finally {
            setIsLoading(false);
        }
    };

    const fetchProblemBoxes = async () => {
        try {
            const data = await ProblemBoxService.getByOrganizationId(orgId);
            setProblemBoxes(data);
        } catch (error) {
            console.error("Failed to fetch problem boxes", error);
        }
    };

    const fetchManagers = async () => {
        try {
            setIsLoading(true);
            const data = await UserService.getAll();
            const filteredManagers = data.filter(u => u.organizationId === orgId);
            setManagers(filteredManagers);
        } catch (error) {
            console.error("Failed to fetch managers", error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleUpdateOrganization = async () => {
        if (!editOrg.name.trim()) return;
        try {
            await OrganizationService.update({
                id: orgId,
                name: editOrg.name,
                adminId: user.id
            });
            setSettingsModalOpen(false);
            fetchOrganization();
        } catch (error) {
            console.error("Failed to update organization", error);
            alert("Failed to update organization");
        }
    };

    const handleDeleteOrganization = async () => {
        if (window.confirm("Are you sure you want to delete this organization? This action cannot be undone.")) {
            try {
                await OrganizationService.delete(orgId);
                navigate('/admin/dashboard');
            } catch (error) {
                console.error("Failed to delete organization", error);
                alert("Failed to delete organization");
            }
        }
    };

    const handleCreateBox = async () => {
        if (!newBox.name.trim() || !newBox.password.trim()) {
            alert("Name and password are required.");
            return;
        }

        try {
            if (newBox.type === 'suggestion') {
                await SuggestionBoxService.create({
                    name: newBox.name,
                    description: newBox.description,
                    isDarkTheme: newBox.isDarkTheme,
                    password: newBox.password,
                    createdBy: user?.id,
                    organizationId: orgId
                });
            } else {
                await ProblemBoxService.create({
                    name: newBox.name,
                    description: newBox.description,
                    isDarkTheme: newBox.isDarkTheme,
                    password: newBox.password,
                    createdBy: newBox.createdBy || user?.username || '',
                    organizationId: orgId,
                    createdAt: new Date().toISOString()
                });
            }

            setBoxModalOpen(false);
            setNewBox({ name: '', type: 'suggestion', description: '', password: '', isDarkTheme: false, createdBy: '' });
            if (newBox.type === 'suggestion') {
                fetchSuggestionBoxes();
            } else {
                fetchProblemBoxes();
            }
            alert(`${newBox.type} box created successfully!`);
        } catch (error) {
            console.error(`Failed to create ${newBox.type} box`, error);
            alert(`Failed to create ${newBox.type} box`);
        }
    };

    const handleCreateManager = async () => {
        try {
            if (!newManager.id.trim()) {
                alert("Please provide a Manager ID");
                return;
            }

            const userToUpdate = await UserService.getById(newManager.id.trim());
            userToUpdate.organizationId = orgId;
            await UserService.update(userToUpdate);

            setManagerModalOpen(false);
            setNewManager({ id: '' });
            fetchManagers();
            alert("Manager assigned successfully!");
        } catch (error) {
            console.error("Failed to assign manager", error);
            alert("Failed to assign manager. Please check the ID.");
        }
    };

    const managerColumns = [
        { header: 'Name', accessor: 'name' },
        { header: 'Email', accessor: 'email' },
        {
            header: 'Role',
            accessor: 'role',
            render: (row) => <StatusBadge type="role" status="Manager" />
        },
        { header: 'Added On', accessor: 'added' }
    ];

    const boxColumns = [
        { header: 'Box Name', accessor: 'name', render: (row) => <strong>{row.name}</strong> },
        {
            header: 'Type',
            accessor: 'type',
            render: (row) => (
                <span className="flex items-center gap-2">
                    {row.type === 'Problem' ? <AlertOctagon size={16} className="text-warning" /> : <MessageSquareWarning size={16} className="text-success" />}
                    {row.type}
                </span>
            )
        },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => <StatusBadge type="status" status={row.status === 'Active' ? 'Open' : 'Closed'} />
        },
        { header: 'Total Reports', accessor: 'count' }
    ];

    return (
        <div className="org-details-container animate-fade-in">
            <div className="back-link" onClick={() => navigate('/admin/dashboard')}>
                <ArrowLeft size={16} /> Back to Dashboard
            </div>

            {/* Organization Header */}
            {isLoading ? (
                <div className="org-header-card glass-panel flex justify-center items-center py-8">
                    <p>Loading organization details...</p>
                </div>
            ) : organization ? (
                <div className="org-header-card glass-panel" style={{ borderLeft: `4px solid ${organization.themeColor || 'var(--accent-primary)'}` }}>
                    <div className="org-icon">
                        {organization.customLogoUrl ? (
                            <img src={organization.customLogoUrl} alt="Logo" style={{ width: 32, height: 32, objectFit: 'contain' }} />
                        ) : (
                            <Building2 size={32} />
                        )}
                    </div>
                    <div className="org-info">
                        <div className="org-title-row">
                            <h1>{organization.name}</h1>
                            <StatusBadge type="status" status="Open" />
                        </div>
                        <p className="org-meta">ID: {organization.id} &bull; Created: {new Date(organization.createdAt || Date.now()).toLocaleDateString()}</p>
                    </div>
                    <div className="org-actions">
                        <div className="notification-bell-wrapper">
                            <button
                                className="btn btn-ghost icon-btn notification-bell-btn"
                                title="Notifications"
                                onClick={() => setShowNotifications(!showNotifications)}
                            >
                                <Bell size={20} />
                                {notifications.length > 0 && (
                                    <span className="notification-badge">{notifications.length}</span>
                                )}
                            </button>

                            {showNotifications && (
                                <div className="notification-dropdown glass-panel">
                                    <div className="notification-dropdown-header">
                                        <h4>Notifications</h4>
                                        <button className="btn btn-ghost icon-btn" onClick={() => setShowNotifications(false)}>
                                            <X size={16} />
                                        </button>
                                    </div>
                                    <div className="notification-dropdown-body">
                                        {notifications.length === 0 ? (
                                            <div className="notification-empty">
                                                No notifications yet.
                                            </div>
                                        ) : (
                                            notifications.map((notif) => (
                                                <div key={notif.id} className="notification-item">
                                                    <div className="notification-item-content">
                                                        <p className="notification-text">{notif.text}</p>
                                                        <span className="notification-time">
                                                            {notif.createdAt ? new Date(notif.createdAt).toLocaleString() : ''}
                                                        </span>
                                                    </div>
                                                    <button
                                                        className="btn btn-ghost icon-btn notification-delete-btn"
                                                        onClick={() => handleDeleteNotification(notif.id)}
                                                        title="Delete notification"
                                                    >
                                                        <Trash2 size={14} />
                                                    </button>
                                                </div>
                                            ))
                                        )}
                                    </div>
                                </div>
                            )}
                        </div>
                        <button className="btn btn-ghost icon-btn" title="Settings" onClick={() => setSettingsModalOpen(true)}>
                            <Settings size={20} />
                        </button>
                    </div>
                </div>
            ) : (
                <div className="org-header-card glass-panel">
                    <p className="text-danger">Organization not found.</p>
                </div>
            )}

            {/* Tabs */}
            <div className="org-tabs">
                <button
                    className={`tab-btn ${activeTab === 'boxes' ? 'active' : ''}`}
                    onClick={() => setActiveTab('boxes')}
                >
                    <MessageSquareWarning size={18} /> Suggestion & Problem Boxes
                </button>
                <button
                    className={`tab-btn ${activeTab === 'managers' ? 'active' : ''}`}
                    onClick={() => setActiveTab('managers')}
                >
                    <Users size={18} /> Managers
                </button>
            </div>

            {/* Tab Content */}
            <div className="tab-content">
                {activeTab === 'boxes' ? (
                    <div className="content-section fade-in">
                        <div className="section-header">
                            <h2>Active Boxes</h2>
                            <button className="btn btn-primary" onClick={() => setBoxModalOpen(true)}>
                                <Plus size={16} /> Create New Box
                            </button>
                        </div>
                        <DataTable
                            data={[
                                ...suggestionBoxes.map(box => ({ ...box, type: 'Suggestion' })),
                                ...problemBoxes.map(box => ({ ...box, type: 'Problem' }))
                            ].filter(box =>
                                box.name?.toLowerCase().includes(boxSearch.toLowerCase()) ||
                                box.description?.toLowerCase().includes(boxSearch.toLowerCase())
                            )}
                            columns={boxColumns}
                            onRowClick={(row) => navigate(`/admin/boxes/${row.id}`)}
                            searchPlaceholder="Search boxes..."
                            searchValue={boxSearch}
                            onSearchChange={setBoxSearch}
                        />
                    </div>
                ) : (
                    <div className="content-section fade-in">
                        <div className="section-header">
                            <h2>Assigned Managers</h2>
                            <button className="btn btn-primary" onClick={() => setManagerModalOpen(true)}>
                                <Plus size={16} /> Add Manager
                            </button>
                        </div>
                        <DataTable
                            data={managers.filter(m =>
                                m.name?.toLowerCase().includes(managerSearch.toLowerCase()) ||
                                m.email?.toLowerCase().includes(managerSearch.toLowerCase())
                            )}
                            columns={managerColumns}
                            onActionClick={(row) => console.log('Manager action', row.id)}
                            searchPlaceholder="Search managers by name or email..."
                            searchValue={managerSearch}
                            onSearchChange={setManagerSearch}
                        />
                    </div>
                )}
            </div>

            {/* Add Manager Modal */}
            <Modal
                isOpen={isManagerModalOpen}
                onClose={() => setManagerModalOpen(false)}
                title="Add New Manager"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setManagerModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={handleCreateManager}>Send Invite</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Manager Id</label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="user id, from user sidebar"
                        value={newManager.id}
                        onChange={(e) => setNewManager({ id: e.target.value })}
                        required
                    />
                </div>
            </Modal>

            {/* Create Box Modal */}
            <Modal
                isOpen={isBoxModalOpen}
                onClose={() => setBoxModalOpen(false)}
                title="Create New Box"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setBoxModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={handleCreateBox}>Create Box</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Box Name *</label>
                    <input
                        type="text"
                        className="form-control"
                        placeholder="e.g. Facilities Feedback"
                        value={newBox.name}
                        onChange={(e) => setNewBox({ ...newBox, name: e.target.value })}
                        required
                    />
                </div>
                <div className="form-group">
                    <label>Box Type *</label>
                    <select
                        className="form-control"
                        value={newBox.type}
                        onChange={(e) => setNewBox({ ...newBox, type: e.target.value })}
                    >
                        <option value="suggestion">Suggestion Box (General feedback & ideas)</option>
                        <option value="problem">Problem Box (Reporting concrete issues)</option>
                    </select>
                </div>
                {newBox.type === 'problem' && (
                    <div className="form-group">
                        <label>Created By</label>
                        <input
                            type="text"
                            className="form-control"
                            placeholder="e.g. Admin User"
                            value={newBox.createdBy}
                            onChange={(e) => setNewBox({ ...newBox, createdBy: e.target.value })}
                        />
                    </div>
                )}
                <div className="form-group">
                    <label>Box Password * (Used by users to access)</label>
                    <input
                        type="password"
                        className="form-control"
                        placeholder="Enter access password"
                        value={newBox.password}
                        onChange={(e) => setNewBox({ ...newBox, password: e.target.value })}
                        required
                    />
                </div>
                <div className="form-group">
                    <label>Theme</label>
                    <div className="flex items-center gap-2 mt-2">
                        <input
                            type="checkbox"
                            id="darkThemeCheck"
                            checked={newBox.isDarkTheme}
                            onChange={(e) => setNewBox({ ...newBox, isDarkTheme: e.target.checked })}
                        />
                        <label htmlFor="darkThemeCheck" className="text-sm m-0">Enable Dark Theme on Public View</label>
                    </div>
                </div>
                <div className="form-group mt-3">
                    <label>Description </label>
                    <textarea
                        className="form-control"
                        rows="3"
                        placeholder="Brief context for users..."
                        value={newBox.description}
                        onChange={(e) => setNewBox({ ...newBox, description: e.target.value })}
                    ></textarea>
                </div>
            </Modal>

            {/* Organization Settings Modal */}
            <Modal
                isOpen={isSettingsModalOpen}
                onClose={() => setSettingsModalOpen(false)}
                title="Organization Settings"
                footer={
                    <div className="flex justify-between w-full">
                        <button className="btn btn-danger" onClick={handleDeleteOrganization}>Delete Organization</button>
                        <div className="flex gap-2">
                            <button className="btn btn-ghost" onClick={() => setSettingsModalOpen(false)}>Cancel</button>
                            <button className="btn btn-primary" onClick={handleUpdateOrganization}>Save Changes</button>
                        </div>
                    </div>
                }
            >
                <div className="form-group">
                    <label>Organization Name *</label>
                    <input
                        type="text"
                        className="form-control"
                        value={editOrg.name}
                        onChange={(e) => setEditOrg({ ...editOrg, name: e.target.value })}
                        required
                    />
                </div>
            </Modal>

        </div>
    );
};

export default OrganizationDetails;
