import React, { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    CreditCard,
    TrendingUp,
    Building2,
    CheckCircle,
    Search,
    ChevronLeft,
    ChevronRight,
    Filter,
    X,
    Eye
} from 'lucide-react';
import { OrganizationService } from '../services/organizationService';
import { SubscriptionService } from '../services/subscriptionService';
import { SubscriptionPlanService } from '../services/subscriptionPlanService';
import { PaymentService } from '../services/paymentService';
import './BillingDashboard.css';

const ITEMS_PER_PAGE = 8;

const BillingDashboard = () => {
    const navigate = useNavigate();

    // Core data
    const [organizations, setOrganizations] = useState([]);
    const [subscriptions, setSubscriptions] = useState([]);
    const [subscriptionPlans, setSubscriptionPlans] = useState([]);
    const [payments, setPayments] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    // Search, filter & pagination
    const [searchQuery, setSearchQuery] = useState('');
    const [filterPlan, setFilterPlan] = useState('all');
    const [showFilters, setShowFilters] = useState(false);
    const [currentPage, setCurrentPage] = useState(1);

    useEffect(() => {
        fetchAllData();
    }, []);

    const fetchAllData = async () => {
        setIsLoading(true);
        try {
            const [orgsData, subsData, plansData, paymentsData] = await Promise.all([
                OrganizationService.getAll(),
                SubscriptionService.getAll(),
                SubscriptionPlanService.getAll(),
                PaymentService.getAll()
            ]);
            setOrganizations(orgsData);
            setSubscriptions(subsData);
            setSubscriptionPlans(plansData);
            setPayments(paymentsData);
        } catch (error) {
            console.error('Failed to fetch billing data', error);
        } finally {
            setIsLoading(false);
        }
    };

    // Build a map of planId -> plan title
    const planMap = useMemo(() => {
        const map = {};
        subscriptionPlans.forEach(p => { map[p.id] = p.title; });
        return map;
    }, [subscriptionPlans]);

    // Build enriched rows: each org + its subscription + plan title
    const enrichedRows = useMemo(() => {
        return organizations.map(org => {
            const sub = subscriptions.find(s => s.organizationId === org.id);
            const planTitle = sub ? (planMap[sub.subscriptionPlanId] || 'Unknown') : 'No Subscription';
            const startDate = sub ? new Date(sub.startDate).toLocaleDateString() : '-';
            const endDate = sub ? new Date(sub.endDate).toLocaleDateString() : '-';
            return {
                ...org,
                subscriptionId: sub?.id || null,
                planTitle,
                startDate,
                endDate,
                subscriptionPlanId: sub?.subscriptionPlanId || null
            };
        });
    }, [organizations, subscriptions, planMap]);

    // Calculate total revenue from active subscriptions (Monthly Recurring Revenue - MRR)
    const totalRevenue = useMemo(() => {
        return subscriptions.reduce((sum, sub) => {
            const planTitle = planMap[sub.subscriptionPlanId]?.toLowerCase() || '';
            let planValue = 0;
            if (planTitle.includes('premium') || planTitle.includes('enterprise')) planValue = 499;
            else if (planTitle.includes('standard') || planTitle.includes('pro')) planValue = 199;
            else if (planTitle.includes('basic')) planValue = 49;

            return sum + planValue;
        }, 0);
    }, [subscriptions, planMap]);

    // Filtered + searched rows
    const filteredRows = useMemo(() => {
        let rows = enrichedRows;

        // Search by org name
        if (searchQuery.trim()) {
            const q = searchQuery.toLowerCase();
            rows = rows.filter(r => r.name?.toLowerCase().includes(q));
        }

        // Filter by plan
        if (filterPlan !== 'all') {
            rows = rows.filter(r => r.planTitle?.toLowerCase() === filterPlan.toLowerCase());
        }

        return rows;
    }, [enrichedRows, searchQuery, filterPlan]);

    // Reset to page 1 on search/filter change
    useEffect(() => {
        setCurrentPage(1);
    }, [searchQuery, filterPlan]);

    // Pagination
    const totalPages = Math.max(1, Math.ceil(filteredRows.length / ITEMS_PER_PAGE));
    const paginatedRows = filteredRows.slice(
        (currentPage - 1) * ITEMS_PER_PAGE,
        currentPage * ITEMS_PER_PAGE
    );

    // Generate page numbers to display
    const getPageNumbers = () => {
        const pages = [];
        const maxVisible = 5;
        let start = Math.max(1, currentPage - Math.floor(maxVisible / 2));
        let end = Math.min(totalPages, start + maxVisible - 1);
        if (end - start + 1 < maxVisible) {
            start = Math.max(1, end - maxVisible + 1);
        }
        for (let i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    };

    // Stats
    const stats = [
        { label: 'Total Subscriptions', value: isLoading ? '...' : subscriptions.length.toString(), icon: CheckCircle, color: 'var(--success)' },
        { label: 'Total Revenue', value: isLoading ? '...' : `€${totalRevenue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, icon: TrendingUp, color: 'var(--accent-primary)' },
        { label: 'Subscription Plans', value: isLoading ? '...' : subscriptionPlans.length.toString(), icon: CreditCard, color: 'var(--warning)' },
        { label: 'Total Organizations', value: isLoading ? '...' : organizations.length.toString(), icon: Building2, color: '#a855f7' },
    ];

    const getPlanBadgeClass = (planTitle) => {
        const lower = planTitle?.toLowerCase() || '';
        if (lower.includes('premium') || lower.includes('enterprise')) return 'premium';
        if (lower.includes('standard') || lower.includes('pro')) return 'standard';
        if (lower.includes('basic')) return 'basic';
        return 'basic';
    };

    return (
        <div className="billing-dashboard animate-fade-in">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Billing & Subscriptions</h1>
                    <p className="page-description">Manage organization plans, track revenue, and monitor subscriptions.</p>
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

            {/* Organization Subscriptions Table */}
            <div className="billing-table-container glass-panel delay-200">
                {/* Table Header */}
                <div className="billing-table-header">
                    <h2 className="billing-table-title">Organization Subscriptions</h2>
                    <div className="billing-table-actions">
                        <div className="billing-search-wrapper">
                            <Search size={18} className="billing-search-icon" />
                            <input
                                type="text"
                                placeholder="Search by organization name..."
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

                {/* Filters Row */}
                {showFilters && (
                    <div className="billing-filters-row animate-fade-in">
                        <div className="billing-filter-group">
                            <label>Subscription Plan</label>
                            <select
                                className="billing-filter-select"
                                value={filterPlan}
                                onChange={(e) => setFilterPlan(e.target.value)}
                            >
                                <option value="all">All Plans</option>
                                {subscriptionPlans.map(plan => (
                                    <option key={plan.id} value={plan.title}>{plan.title}</option>
                                ))}
                                <option value="No Subscription">No Subscription</option>
                            </select>
                        </div>
                        {filterPlan !== 'all' && (
                            <button className="billing-clear-filters" onClick={() => setFilterPlan('all')}>
                                <X size={14} /> Clear Filters
                            </button>
                        )}
                    </div>
                )}

                {/* Table */}
                <div className="billing-table-responsive">
                    <table className="billing-table">
                        <thead>
                            <tr>
                                <th>Organization</th>
                                <th>Plan</th>
                                <th>Start Date</th>
                                <th>End Date</th>
                                <th style={{ width: '80px', textAlign: 'center' }}>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {isLoading ? (
                                <tr>
                                    <td colSpan="5" className="billing-empty-state">
                                        <div className="billing-loading-spinner"></div>
                                        Loading billing data...
                                    </td>
                                </tr>
                            ) : paginatedRows.length === 0 ? (
                                <tr>
                                    <td colSpan="5" className="billing-empty-state">
                                        {searchQuery || filterPlan !== 'all'
                                            ? 'No organizations match your search or filter criteria.'
                                            : 'No organizations found.'}
                                    </td>
                                </tr>
                            ) : (
                                paginatedRows.map((row) => (
                                    <tr key={row.id} className="billing-table-row">
                                        <td>
                                            <div className="billing-org-cell">
                                                <div className="billing-org-avatar" style={{ background: row.themeColor || '#6366f1' }}>
                                                    {row.name?.charAt(0).toUpperCase() || '?'}
                                                </div>
                                                <div>
                                                    <strong className="billing-org-name">{row.name}</strong>
                                                    <span className="billing-org-id">ID: {row.id?.slice(0, 8)}...</span>
                                                </div>
                                            </div>
                                        </td>
                                        <td>
                                            <span className={`plan-badge ${getPlanBadgeClass(row.planTitle)}`}>
                                                {row.planTitle}
                                            </span>
                                        </td>
                                        <td className="billing-date-cell">{row.startDate}</td>
                                        <td className="billing-date-cell">{row.endDate}</td>
                                        <td style={{ textAlign: 'center' }}>
                                            {row.subscriptionId ? (
                                                <button
                                                    className="btn btn-ghost icon-btn small"
                                                    title="View subscription details"
                                                    onClick={() => navigate(`/admin/billing/${row.id}`)}
                                                >
                                                    <Eye size={16} />
                                                </button>
                                            ) : (
                                                <span className="billing-no-action">—</span>
                                            )}
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>

                {/* Pagination */}
                {!isLoading && filteredRows.length > 0 && (
                    <div className="billing-pagination">
                        <span className="billing-pagination-info">
                            Showing {((currentPage - 1) * ITEMS_PER_PAGE) + 1} to {Math.min(currentPage * ITEMS_PER_PAGE, filteredRows.length)} of {filteredRows.length} entries
                        </span>
                        <div className="billing-pagination-controls">
                            <button
                                className="btn btn-ghost icon-btn small"
                                disabled={currentPage <= 1}
                                onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
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
                                onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                            >
                                <ChevronRight size={16} />
                            </button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default BillingDashboard;
