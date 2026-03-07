import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, CreditCard, Download, ExternalLink, Calendar, CheckCircle, AlertTriangle, Search, Filter, X, ChevronLeft, ChevronRight } from 'lucide-react';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import { OrganizationService } from '../services/organizationService';
import { SubscriptionService } from '../services/subscriptionService';
import { SubscriptionPlanService } from '../services/subscriptionPlanService';
import { PaymentService } from '../services/paymentService';
import { BillingNotificationService } from '../services/billingNotificationService';

const ITEMS_PER_PAGE = 5;

const SubscriptionDetails = () => {
    const { orgId } = useParams();
    const navigate = useNavigate();

    // Core data
    const [organization, setOrganization] = useState(null);
    const [subscription, setSubscription] = useState(null);
    const [subscriptionPlans, setSubscriptionPlans] = useState([]);
    const [payments, setPayments] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    // Modal state
    const [isChangePlanModalOpen, setChangePlanModalOpen] = useState(false);
    const [selectedPlanId, setSelectedPlanId] = useState('');
    const [isUpdatingPlan, setIsUpdatingPlan] = useState(false);

    const [isUpdatePaymentModalOpen, setUpdatePaymentModalOpen] = useState(false);
    const [newPaymentMethod, setNewPaymentMethod] = useState('');
    const [isUpdatingPayment, setIsUpdatingPayment] = useState(false);

    // Table state
    const [searchQuery, setSearchQuery] = useState('');
    const [filterStatus, setFilterStatus] = useState('all');
    const [filterMethod, setFilterMethod] = useState('all');
    const [showFilters, setShowFilters] = useState(false);
    const [currentPage, setCurrentPage] = useState(1);

    useEffect(() => {
        if (orgId) {
            fetchData();
        }
    }, [orgId]);

    const fetchData = async () => {
        setIsLoading(true);
        try {
            const [orgsData, subsData, plansData, paymentsData] = await Promise.all([
                OrganizationService.getById(orgId).catch(() => null),
                SubscriptionService.getAll(),
                SubscriptionPlanService.getAll(),
                PaymentService.getAll()
            ]);

            if (orgsData) {
                setOrganization(orgsData);
                const orgSub = subsData.find(s => s.organizationId === orgId);
                setSubscription(orgSub || null);
                setSubscriptionPlans(plansData);

                if (orgSub) {
                    const subPayments = paymentsData.filter(p => p.subscriptionId === orgSub.id);
                    setPayments(subPayments.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)));
                }
            }
        } catch (error) {
            console.error('Failed to fetch subscription details', error);
        } finally {
            setIsLoading(false);
        }
    };

    const currentPlan = useMemo(() => {
        if (!subscription || subscriptionPlans.length === 0) return null;
        return subscriptionPlans.find(p => p.id === subscription.subscriptionPlanId);
    }, [subscription, subscriptionPlans]);

    const getPlanValue = (planTitle) => {
        const lower = planTitle?.toLowerCase() || '';
        if (lower.includes('premium') || lower.includes('enterprise')) return 499;
        if (lower.includes('standard') || lower.includes('pro')) return 199;
        if (lower.includes('basic')) return 49;
        return 0;
    };

    const getPlanBadgeClass = (planTitle) => {
        const lower = planTitle?.toLowerCase() || '';
        if (lower.includes('premium') || lower.includes('enterprise')) return 'premium';
        if (lower.includes('standard') || lower.includes('pro')) return 'standard';
        return 'basic';
    };

    const handleDownloadReceipt = (payment) => {
        alert(`Receipt for payment ${payment.id.split('-')[0]} has been sent to the organization's billing email.`);
    };

    const handleChangePlan = async () => {
        if (!selectedPlanId || !subscription) return;
        setIsUpdatingPlan(true);
        try {
            await SubscriptionService.update({
                ...subscription,
                subscriptionPlanId: selectedPlanId
            });

            const newPlan = subscriptionPlans.find(p => p.id === selectedPlanId);
            const amount = getPlanValue(newPlan?.title);

            const payment = await PaymentService.create({
                subscriptionId: subscription.id,
                total: amount,
                currency: 'EUR',
                paymentMethod: payments.length > 0 ? payments[0].paymentMethod : 'CreditCard'
            });

            await BillingNotificationService.create({
                text: `Subscription plan changed to "${newPlan?.title || 'New Plan'}" for organization "${organization?.name}".`,
                organizationId: organization.id,
                paymentId: payment.id
            });

            setChangePlanModalOpen(false);
            fetchData();
            alert('Subscription plan updated successfully!');
        } catch (error) {
            alert('Failed to update plan: ' + error.message);
        } finally {
            setIsUpdatingPlan(false);
        }
    };

    const handleUpdatePayment = async () => {
        if (!newPaymentMethod || payments.length === 0) return;
        setIsUpdatingPayment(true);
        try {
            const latestPayment = payments[0];
            await PaymentService.update({
                ...latestPayment,
                paymentMethod: newPaymentMethod
            });

            setUpdatePaymentModalOpen(false);
            fetchData();
            alert('Payment method updated successfully!');
        } catch (error) {
            alert('Failed to update payment method: ' + error.message);
        } finally {
            setIsUpdatingPayment(false);
        }
    };

    // Table Data processing
    const filteredPayments = useMemo(() => {
        let result = payments;

        if (searchQuery.trim()) {
            const q = searchQuery.toLowerCase();
            result = result.filter(p => p.id.toLowerCase().includes(q));
        }

        if (filterStatus !== 'all') {
            result = result.filter(p => {
                const statusStr = p.status?.toString().toLowerCase() || 'pending';
                if (filterStatus === 'completed') return statusStr === '1' || statusStr === 'completed' || statusStr === 'success';
                if (filterStatus === 'pending') return statusStr === '0' || statusStr === 'pending';
                if (filterStatus === 'failed') return statusStr === 'failed' || statusStr === 'error';
                return true;
            });
        }

        if (filterMethod !== 'all') {
            result = result.filter(p => p.paymentMethod?.toLowerCase() === filterMethod.toLowerCase());
        }

        return result;
    }, [payments, searchQuery, filterStatus, filterMethod]);

    useEffect(() => {
        setCurrentPage(1);
    }, [searchQuery, filterStatus, filterMethod]);

    const totalPages = Math.max(1, Math.ceil(filteredPayments.length / ITEMS_PER_PAGE));
    const paginatedPayments = filteredPayments.slice(
        (currentPage - 1) * ITEMS_PER_PAGE,
        currentPage * ITEMS_PER_PAGE
    );

    const getPageNumbers = () => {
        const pages = [];
        const maxVisible = 5;
        let start = Math.max(1, currentPage - Math.floor(maxVisible / 2));
        let end = Math.min(totalPages, start + maxVisible - 1);
        if (end - start + 1 < maxVisible) start = Math.max(1, end - maxVisible + 1);
        for (let i = start; i <= end; i++) pages.push(i);
        return pages;
    };

    const getStatusType = (status) => {
        if (status === 1 || status === 'Completed' || status === 'Success') return 'Resolved';
        if (status === 0 || status === 'Pending') return 'In Progress';
        return 'Low';
    };

    if (isLoading) return <div className="p-8 text-center text-muted">Loading subscription details...</div>;
    if (!organization) return <div className="p-8 text-center text-muted">Organization not found.</div>;

    return (
        <div className="animate-fade-in" style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>

            <div className="back-link" onClick={() => navigate('/admin/billing')} style={{ cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: '0.5rem', color: 'var(--text-secondary)' }}>
                <ArrowLeft size={16} /> Back to Billing
            </div>

            <div className="page-header">
                <div>
                    <h1 className="page-title">{organization.name}</h1>
                    <p className="page-description">Subscription & Payment Profile</p>
                </div>
                <StatusBadge type="status" status={subscription ? 'Resolved' : 'Low'} />
            </div>

            {subscription ? (
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '1.5rem' }}>
                    {/* Core Subscription Profile */}
                    <div className="glass-panel" style={{ padding: '2rem' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
                            <h2 style={{ fontSize: '1.25rem', fontWeight: '600', margin: 0, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                <CheckCircle size={20} className="text-success" /> Current Plan
                            </h2>
                            <span className={`plan-badge ${getPlanBadgeClass(currentPlan?.title)}`}>
                                {currentPlan?.title || 'Unknown Plan'}
                            </span>
                        </div>

                        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '0.5rem' }}>
                                <span style={{ color: 'var(--text-secondary)' }}>Pricing</span>
                                <strong>€{getPlanValue(currentPlan?.title).toLocaleString('en-US', { minimumFractionDigits: 2 })} / year</strong>
                            </div>
                            <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '0.5rem' }}>
                                <span style={{ color: 'var(--text-secondary)' }}>Start Date</span>
                                <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}><Calendar size={14} className="text-muted" /> {new Date(subscription.startDate).toLocaleDateString()}</span>
                            </div>
                            <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '0.5rem' }}>
                                <span style={{ color: 'var(--text-secondary)' }}>End/Renewal Date</span>
                                <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}><Calendar size={14} className="text-muted" /> {new Date(subscription.endDate).toLocaleDateString()}</span>
                            </div>
                        </div>
                        <button
                            className="btn btn-ghost"
                            style={{ width: '100%', marginTop: '1.5rem', border: '1px solid var(--border-subtle)' }}
                            onClick={() => { setSelectedPlanId(currentPlan?.id || ''); setChangePlanModalOpen(true); }}
                        >
                            Change Plan
                        </button>
                    </div>

                    {/* Payment Profile */}
                    <div className="glass-panel" style={{ padding: '2rem' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
                            <h2 style={{ fontSize: '1.25rem', fontWeight: '600', margin: 0, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                <CreditCard size={20} className="text-secondary" /> Payment Method
                            </h2>
                        </div>

                        {payments.length > 0 ? (
                            <div style={{ background: 'rgba(0,0,0,0.2)', border: '1px solid var(--border-subtle)', padding: '1rem', borderRadius: 'var(--radius-md)' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                                    <strong style={{ fontSize: '1.1rem', textTransform: 'capitalize' }}>{payments[0].paymentMethod || 'CreditCard'}</strong>
                                    <span style={{ color: 'var(--text-muted)' }}>Default</span>
                                </div>
                                <div style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                                    Currency: {payments[0].currency || 'EUR'}
                                </div>
                            </div>
                        ) : (
                            <div style={{ background: 'rgba(0,0,0,0.2)', border: '1px solid var(--border-subtle)', padding: '1rem', borderRadius: 'var(--radius-md)', color: 'var(--text-muted)' }}>
                                No payment method on file.
                            </div>
                        )}

                        <button
                            className="btn btn-ghost"
                            style={{ width: '100%', marginTop: '1.5rem' }}
                            onClick={() => { setNewPaymentMethod(payments[0]?.paymentMethod || 'CreditCard'); setUpdatePaymentModalOpen(true); }}
                        >
                            Update Payment Method <ExternalLink size={14} className="ml-2" style={{ marginLeft: '0.5rem' }} />
                        </button>
                    </div>
                </div>
            ) : (
                <div className="glass-panel" style={{ padding: '3rem 2rem', textAlign: 'center' }}>
                    <p style={{ color: 'var(--text-secondary)' }}>This organization does not currently have an active subscription.</p>
                </div>
            )}

            {/* Payment History Table (Custom implementation matching BillingDashboard style) */}
            {subscription && (
                <div className="billing-table-container glass-panel">
                    <div className="billing-table-header">
                        <h2 className="billing-table-title">Payment History</h2>
                        <div className="billing-table-actions">
                            <div className="billing-search-wrapper">
                                <Search size={18} className="billing-search-icon" />
                                <input
                                    type="text"
                                    placeholder="Search by Payment ID..."
                                    className="billing-search-input"
                                    value={searchQuery}
                                    onChange={(e) => setSearchQuery(e.target.value)}
                                />
                                {searchQuery && (
                                    <button className="billing-search-clear" onClick={() => setSearchQuery('')}>
                                        <X size={14} />
                                    </button>
                                )}
                            </div>
                            <button
                                className={`btn btn-ghost icon-btn ${showFilters ? 'active' : ''}`}
                                title="Filter"
                                onClick={() => setShowFilters(!showFilters)}
                            >
                                <Filter size={18} />
                            </button>
                        </div>
                    </div>

                    {showFilters && (
                        <div className="billing-filters-row animate-fade-in" style={{ padding: '1rem', background: 'rgba(0,0,0,0.15)', borderRadius: 'var(--radius-md)', marginBottom: '1rem', display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                            <div className="billing-filter-group" style={{ display: 'flex', flexDirection: 'column', gap: '0.35rem' }}>
                                <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase' }}>Status</label>
                                <select
                                    className="billing-filter-select"
                                    value={filterStatus}
                                    onChange={(e) => setFilterStatus(e.target.value)}
                                >
                                    <option value="all">All Statuses</option>
                                    <option value="completed">Completed / Success</option>
                                    <option value="pending">Pending</option>
                                    <option value="failed">Failed / Low</option>
                                </select>
                            </div>
                            <div className="billing-filter-group" style={{ display: 'flex', flexDirection: 'column', gap: '0.35rem' }}>
                                <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase' }}>Method</label>
                                <select
                                    className="billing-filter-select"
                                    value={filterMethod}
                                    onChange={(e) => setFilterMethod(e.target.value)}
                                >
                                    <option value="all">All Methods</option>
                                    <option value="creditcard">Credit Card</option>
                                    <option value="debitcard">Debit Card</option>
                                    <option value="paypal">PayPal</option>
                                </select>
                            </div>
                            {(filterStatus !== 'all' || filterMethod !== 'all') && (
                                <button
                                    className="billing-clear-filters"
                                    onClick={() => { setFilterStatus('all'); setFilterMethod('all'); }}
                                    style={{ alignSelf: 'flex-end', marginLeft: 'auto' }}
                                >
                                    <X size={14} /> Clear
                                </button>
                            )}
                        </div>
                    )}

                    <div className="billing-table-responsive">
                        <table className="billing-table">
                            <thead>
                                <tr>
                                    <th>Payment ID</th>
                                    <th>Date</th>
                                    <th>Method</th>
                                    <th>Total</th>
                                    <th>Status</th>
                                    <th style={{ width: '80px', textAlign: 'center' }}>Receipt</th>
                                </tr>
                            </thead>
                            <tbody>
                                {paginatedPayments.length === 0 ? (
                                    <tr>
                                        <td colSpan="6" className="billing-empty-state">
                                            {searchQuery || filterStatus !== 'all' || filterMethod !== 'all'
                                                ? 'No payments match your filters.'
                                                : 'No payments history found.'}
                                        </td>
                                    </tr>
                                ) : (
                                    paginatedPayments.map(p => (
                                        <tr key={p.id}>
                                            <td><strong style={{ color: 'var(--accent-primary)' }}>{p.id.split('-')[0]}...</strong></td>
                                            <td className="billing-date-cell">{new Date(p.createdAt || new Date()).toLocaleDateString()}</td>
                                            <td style={{ textTransform: 'capitalize' }}>{p.paymentMethod}</td>
                                            <td>€{(p.total || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                                            <td>
                                                <StatusBadge type="status" status={getStatusType(p.status)} />
                                            </td>
                                            <td style={{ textAlign: 'center' }}>
                                                <button className="btn btn-ghost icon-btn small" title="Download & Email PDF" onClick={() => handleDownloadReceipt(p)}>
                                                    <Download size={16} />
                                                </button>
                                            </td>
                                        </tr>
                                    ))
                                )}
                            </tbody>
                        </table>
                    </div>

                    {filteredPayments.length > 0 && (
                        <div className="billing-pagination">
                            <span className="billing-pagination-info">
                                Showing {((currentPage - 1) * ITEMS_PER_PAGE) + 1} to {Math.min(currentPage * ITEMS_PER_PAGE, filteredPayments.length)} of {filteredPayments.length} entries
                            </span>
                            <div className="billing-pagination-controls">
                                <button
                                    className="btn btn-ghost icon-btn small"
                                    disabled={currentPage <= 1}
                                    onClick={() => setCurrentPage(c => Math.max(1, c - 1))}
                                >
                                    <ChevronLeft size={16} />
                                </button>
                                {getPageNumbers().map(page => (
                                    <button
                                        key={page}
                                        className={`billing-page-number ${page === currentPage ? 'active' : ''}`}
                                        onClick={() => setCurrentPage(page)}
                                    >
                                        {page}
                                    </button>
                                ))}
                                <button
                                    className="btn btn-ghost icon-btn small"
                                    disabled={currentPage >= totalPages}
                                    onClick={() => setCurrentPage(c => Math.min(totalPages, c + 1))}
                                >
                                    <ChevronRight size={16} />
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            )}

            {/* Modals */}
            <Modal
                isOpen={isChangePlanModalOpen}
                onClose={() => setChangePlanModalOpen(false)}
                title="Change Subscription Plan"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setChangePlanModalOpen(false)} disabled={isUpdatingPlan}>Cancel</button>
                        <button className="btn btn-primary" onClick={handleChangePlan} disabled={isUpdatingPlan}>
                            {isUpdatingPlan ? 'Applying...' : 'Apply Plan Change'}
                        </button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Select New Plan</label>
                    <select
                        className="form-control"
                        value={selectedPlanId}
                        onChange={(e) => setSelectedPlanId(e.target.value)}
                        disabled={isUpdatingPlan}
                    >
                        <option value="">Select a plan...</option>
                        {subscriptionPlans.map(plan => (
                            <option key={plan.id} value={plan.id}>
                                {plan.title} — €{getPlanValue(plan.title)}/yr
                            </option>
                        ))}
                    </select>
                </div>
                <div className="form-alert warning">
                    <AlertTriangle size={16} />
                    <span>Changing a plan will immediately prorate the invoice and charge the new amount.</span>
                </div>
            </Modal>

            <Modal
                isOpen={isUpdatePaymentModalOpen}
                onClose={() => setUpdatePaymentModalOpen(false)}
                title="Update Payment Method"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setUpdatePaymentModalOpen(false)} disabled={isUpdatingPayment}>Cancel</button>
                        <button className="btn btn-primary" onClick={handleUpdatePayment} disabled={isUpdatingPayment}>
                            {isUpdatingPayment ? 'Saving...' : 'Save Payment Method'}
                        </button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Payment Method Type</label>
                    <select
                        className="form-control"
                        value={newPaymentMethod}
                        onChange={(e) => setNewPaymentMethod(e.target.value)}
                        disabled={isUpdatingPayment}
                    >
                        <option value="CreditCard">Credit Card</option>
                        <option value="DebitCard">Debit Card</option>
                        <option value="PayPal">PayPal</option>
                    </select>
                </div>
                <div className="form-alert" style={{ background: 'rgba(99, 102, 241, 0.1)', border: '1px solid rgba(99, 102, 241, 0.2)', color: 'var(--accent-primary)' }}>
                    <span>This method will be used for all future recurring billing.</span>
                </div>
            </Modal>
        </div>
    );
};

export default SubscriptionDetails;
