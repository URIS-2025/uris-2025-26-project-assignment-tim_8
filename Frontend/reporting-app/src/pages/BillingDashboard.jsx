import React, { useState } from 'react';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { CreditCard, TrendingUp, Building2, CheckCircle, AlertTriangle, Edit } from 'lucide-react';
import './BillingDashboard.css';

// Mock Data
const mockBillingStats = [
    { label: 'Active Subscriptions', value: '45', icon: CheckCircle, color: 'var(--success)' },
    { label: 'Monthly Recurring Revenue', value: '$12,450', icon: TrendingUp, color: 'var(--accent-primary)' },
    { label: 'Pending Renewals (30 days)', value: '8', icon: AlertTriangle, color: 'var(--warning)' },
    { label: 'Total Organizations', value: '52', icon: Building2, color: '#a855f7' },
];

const mockSubscriptions = [
    { id: 'ORG-001', orgName: 'Tech Corp International', plan: 'Enterprise', status: 'Active', nextBilling: 'Nov 15, 2023', amount: '$499/mo' },
    { id: 'ORG-002', orgName: 'Design Studio LLC', plan: 'Pro', status: 'Active', nextBilling: 'Nov 01, 2023', amount: '$199/mo' },
    { id: 'ORG-003', orgName: 'Local University', plan: 'Enterprise', status: 'Past Due', nextBilling: 'Oct 15, 2023', amount: '$499/mo' },
    { id: 'ORG-004', orgName: 'Small Biz Inc', plan: 'Basic', status: 'Active', nextBilling: 'Dec 01, 2023', amount: '$49/mo' },
    { id: 'ORG-005', orgName: 'Global Logistics', plan: 'Pro', status: 'Canceled', nextBilling: '-', amount: '-' },
];

const BillingDashboard = () => {
    const [isEditModalOpen, setEditModalOpen] = useState(false);
    const [selectedOrg, setSelectedOrg] = useState(null);

    const handleEditClick = (row) => {
        setSelectedOrg(row);
        setEditModalOpen(true);
    };

    const columns = [
        { header: 'Organization', accessor: 'orgName', render: (row) => <strong>{row.orgName}</strong> },
        {
            header: 'Plan',
            accessor: 'plan',
            render: (row) => (
                <span className={`plan-badge ${row.plan.toLowerCase()}`}>{row.plan}</span>
            )
        },
        { header: 'Amount', accessor: 'amount' },
        { header: 'Next Billing', accessor: 'nextBilling' },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => (
                <StatusBadge
                    type="status"
                    status={row.status === 'Past Due' ? 'In Progress' : row.status === 'Canceled' ? 'Low' : 'Resolved'}
                // Reusing status badge colors implicitly: Past Due (Warning/Yellow), Canceled (Gray), Active (Success/Green)
                // Wait, actually I will just use explicit coloring for billing statuses
                />
            )
        },
        {
            header: 'Actions',
            accessor: 'actions',
            render: (row) => (
                <button className="btn btn-ghost icon-btn small" onClick={() => handleEditClick(row)}>
                    <Edit size={16} />
                </button>
            )
        }
    ];

    return (
        <div className="billing-dashboard animate-fade-in">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Billing & Subscriptions</h1>
                    <p className="page-description">Manage organization plans, track revenue, and handle billing issues.</p>
                </div>
            </div>

            <div className="stats-grid">
                {mockBillingStats.map((stat, index) => (
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

            <div className="table-wrapper delay-200">
                <DataTable
                    title="Organization Subscriptions"
                    data={mockSubscriptions}
                    columns={columns}
                    searchPlaceholder="Search by organization name or plan..."
                />
            </div>

            {/* Edit Subscription Modal */}
            <Modal
                isOpen={isEditModalOpen}
                onClose={() => setEditModalOpen(false)}
                title={`Edit Subscription: ${selectedOrg?.orgName}`}
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setEditModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={() => setEditModalOpen(false)}>Update Plan</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Subscription Plan</label>
                    <select className="form-control" defaultValue={selectedOrg?.plan}>
                        <option value="Basic">Basic ($49/mo)</option>
                        <option value="Pro">Pro ($199/mo)</option>
                        <option value="Enterprise">Enterprise ($499/mo)</option>
                    </select>
                </div>
                <div className="form-group">
                    <label>Status</label>
                    <select className="form-control" defaultValue={selectedOrg?.status}>
                        <option value="Active">Active</option>
                        <option value="Past Due">Past Due</option>
                        <option value="Canceled">Canceled</option>
                    </select>
                </div>
                <div className="form-alert warning">
                    <AlertTriangle size={16} />
                    <span>Changing a plan will immediately prune their quotas and send a prorated invoice.</span>
                </div>
            </Modal>
        </div>
    );
};

export default BillingDashboard;
