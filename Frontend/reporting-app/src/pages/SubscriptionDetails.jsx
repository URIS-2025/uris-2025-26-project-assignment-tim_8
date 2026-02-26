import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, CreditCard, Download, ExternalLink, Calendar, CheckCircle } from 'lucide-react';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';

const mockSubscription = {
    id: 'ORG-001',
    orgName: 'Tech Corp International',
    plan: 'Enterprise',
    status: 'Active',
    price: '$499',
    billingCycle: 'Monthly',
    nextBilling: 'Nov 15, 2023',
    paymentMethod: {
        type: 'Credit Card',
        last4: '4242',
        expire: '12/25'
    },
    history: [
        { id: 'INV-101', date: 'Oct 15, 2023', amount: '$499.00', status: 'Paid' },
        { id: 'INV-100', date: 'Sep 15, 2023', amount: '$499.00', status: 'Paid' },
        { id: 'INV-099', date: 'Aug 15, 2023', amount: '$499.00', status: 'Paid' },
    ]
};

const SubscriptionDetails = () => {
    // const { orgId } = useParams();
    const navigate = useNavigate();

    const columns = [
        { header: 'Invoice ID', accessor: 'id', render: (row) => <strong style={{ color: 'var(--accent-primary)' }}>{row.id}</strong> },
        { header: 'Date', accessor: 'date' },
        { header: 'Amount', accessor: 'amount' },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => <StatusBadge type="status" status={row.status === 'Paid' ? 'Resolved' : 'In Progress'} />
        },
        {
            header: 'Receipt',
            accessor: 'receipt',
            render: (row) => (
                <button className="btn btn-ghost icon-btn small" title="Download PDF">
                    <Download size={16} />
                </button>
            )
        }
    ];

    return (
        <div className="animate-fade-in" style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>

            <div className="back-link" onClick={() => navigate('/admin/billing')} style={{ cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: '0.5rem', color: 'var(--text-secondary)' }}>
                <ArrowLeft size={16} /> Back to Billing
            </div>

            <div className="page-header">
                <div>
                    <h1 className="page-title">{mockSubscription.orgName}</h1>
                    <p className="page-description">Subscription & Payment Profile</p>
                </div>
                <StatusBadge type="status" status="Resolved" />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '1.5rem' }}>

                {/* Core Subscription Profile */}
                <div className="glass-panel" style={{ padding: '2rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
                        <h2 style={{ fontSize: '1.25rem', fontWeight: '600', margin: 0, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <CheckCircle size={20} className="text-success" /> Current Plan
                        </h2>
                        <span className="plan-badge enterprise" style={{ padding: '0.25rem 0.75rem', borderRadius: 'var(--radius-sm)', background: 'rgba(168, 85, 247, 0.2)', color: '#c084fc', border: '1px solid rgba(168, 85, 247, 0.3)', fontSize: '0.8rem', fontWeight: 'bold', textTransform: 'uppercase' }}>
                            {mockSubscription.plan}
                        </span>
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '0.5rem' }}>
                            <span style={{ color: 'var(--text-secondary)' }}>Pricing</span>
                            <strong>{mockSubscription.price} / {mockSubscription.billingCycle}</strong>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '0.5rem' }}>
                            <span style={{ color: 'var(--text-secondary)' }}>Next Billing Date</span>
                            <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}><Calendar size={14} className="text-muted" /> {mockSubscription.nextBilling}</span>
                        </div>
                    </div>
                    <button className="btn btn-ghost" style={{ width: '100%', marginTop: '1.5rem', border: '1px solid var(--border-subtle)' }}>Change Plan</button>
                </div>

                {/* Payment Profile */}
                <div className="glass-panel" style={{ padding: '2rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
                        <h2 style={{ fontSize: '1.25rem', fontWeight: '600', margin: 0, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <CreditCard size={20} className="text-secondary" /> Payment Method
                        </h2>
                    </div>

                    <div style={{ background: 'rgba(0,0,0,0.2)', border: '1px solid var(--border-subtle)', padding: '1rem', borderRadius: 'var(--radius-md)' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                            <strong style={{ fontSize: '1.1rem' }}>•••• •••• •••• {mockSubscription.paymentMethod.last4}</strong>
                            <span style={{ color: 'var(--text-muted)' }}>{mockSubscription.paymentMethod.type}</span>
                        </div>
                        <div style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                            Expires {mockSubscription.paymentMethod.expire}
                        </div>
                    </div>

                    <button className="btn btn-ghost" style={{ width: '100%', marginTop: '1.5rem' }}>Update Payment Method <ExternalLink size={14} className="ml-2" style={{ marginLeft: '0.5rem' }} /></button>
                </div>
            </div>

            <div className="table-wrapper">
                <DataTable
                    title="Payment History"
                    data={mockSubscription.history}
                    columns={columns}
                    searchPlaceholder="Search invoices..."
                />
            </div>

        </div>
    );
};

export default SubscriptionDetails;
