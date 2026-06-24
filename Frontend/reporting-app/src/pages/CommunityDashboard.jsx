import React, { useState, useEffect } from 'react';
import { Loader2 } from 'lucide-react';
import { SuggestionService } from '../services/suggestionService';
import { OrganizationService } from '../services/organizationService';
import { SuggestionBoxService } from '../services/suggestionBoxService';
import CommunityFeed from '../components/Community/CommunityFeed';
import { useAuth } from '../context/AuthContext';
import './CommunityDashboard.css';

const CommunityDashboard = () => {
    const [suggestions, setSuggestions] = useState([]);
    const [loading, setLoading] = useState(false);
    const { user } = useAuth();
    const isManager = (user?.role || 'user') === 'manager';
    const orgId = user?.organizationId || null;

    // Organization & box selection
    const [organizations, setOrganizations] = useState([]);
    const [selectedOrgId, setSelectedOrgId] = useState('');
    const [suggestionBoxes, setSuggestionBoxes] = useState([]);
    const [selectedBoxId, setSelectedBoxId] = useState('');
    const [loadingBoxes, setLoadingBoxes] = useState(false);

    // Fetch organizations on mount (admin only). Managers are scoped to their own org,
    // so there is no org picker — we just pin the selection to their organizationId.
    useEffect(() => {
        if (isManager) {
            if (orgId) setSelectedOrgId(orgId);
            return;
        }
        const fetchOrgs = async () => {
            try {
                const orgs = await OrganizationService.getAll();
                setOrganizations(orgs);
            } catch (err) {
                console.error('Error fetching organizations:', err);
            }
        };
        fetchOrgs();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // When organization changes, fetch suggestion boxes for that org
    useEffect(() => {
        if (!selectedOrgId) {
            setSuggestionBoxes([]);
            setSelectedBoxId('');
            setSuggestions([]);
            return;
        }

        const fetchBoxes = async () => {
            try {
                setLoadingBoxes(true);
                const boxes = await SuggestionBoxService.getByOrganizationId(selectedOrgId);
                setSuggestionBoxes(boxes);
                if (boxes.length > 0) {
                    setSelectedBoxId(boxes[0].id);
                } else {
                    setSelectedBoxId('');
                    setSuggestions([]);
                }
            } catch (err) {
                console.error('Error fetching suggestion boxes:', err);
                setSuggestionBoxes([]);
                setSelectedBoxId('');
            } finally {
                setLoadingBoxes(false);
            }
        };
        fetchBoxes();
    }, [selectedOrgId]);

    // When suggestion box changes, fetch suggestions filtered by that box
    useEffect(() => {
        const fetchSuggestions = async () => {
            if (!selectedBoxId) {
                setSuggestions([]);
                return;
            }
            try {
                setLoading(true);
                const allSuggestions = await SuggestionService.getAll();
                // Filter by selected suggestion box
                const filtered = allSuggestions.filter(s => s.suggestionBoxId === selectedBoxId);
                setSuggestions(filtered);
            } catch (err) {
                console.error('Error fetching suggestions:', err);
            } finally {
                setLoading(false);
            }
        };
        fetchSuggestions();
    }, [selectedBoxId]);

    const adminAnonUserId = user?.id || '00000000-0000-0000-0000-000000000000';

    return (
        <div className="page-container animate-fade-in">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Community Board</h1>
                    <p className="page-subtitle">View and interact with all anonymous suggestions and feedback</p>
                </div>
            </div>

            {/* Filters */}
            <div className="glass-panel cd-filters">
                {!isManager && (
                    <div className="cd-filter-field">
                        <label className="cd-filter-label">
                            Organization
                        </label>
                        <select
                            className="form-control"
                            value={selectedOrgId}
                            onChange={(e) => setSelectedOrgId(e.target.value)}
                        >
                            <option value="">— Select an organization —</option>
                            {organizations.map(org => (
                                <option key={org.id} value={org.id}>{org.name}</option>
                            ))}
                        </select>
                    </div>
                )}

                <div className="cd-filter-field">
                    <label className="cd-filter-label">
                        Suggestion Box
                    </label>
                    {!selectedOrgId ? (
                        <select className="form-control" disabled>
                            <option>Select an organization first</option>
                        </select>
                    ) : loadingBoxes ? (
                        <select className="form-control" disabled>
                            <option>Loading boxes...</option>
                        </select>
                    ) : suggestionBoxes.length > 0 ? (
                        <select
                            className="form-control"
                            value={selectedBoxId}
                            onChange={(e) => setSelectedBoxId(e.target.value)}
                        >
                            {suggestionBoxes.map(box => (
                                <option key={box.id} value={box.id}>
                                    {box.name}{box.description ? ` — ${box.description}` : ''}
                                </option>
                            ))}
                        </select>
                    ) : (
                        <select className="form-control" disabled>
                            <option>No suggestion boxes found</option>
                        </select>
                    )}
                </div>
            </div>

            <div className="page-content cd-content">
                {!selectedOrgId ? (
                    <div className="cd-state">
                        <p>{isManager ? 'No organization is assigned to your account.' : 'Please select an organization to view suggestions.'}</p>
                    </div>
                ) : !selectedBoxId ? (
                    <div className="cd-state">
                        <p>Please select a suggestion box to view suggestions.</p>
                    </div>
                ) : loading ? (
                    <div className="cd-state">
                        <Loader2 size={32} className="cd-state-icon" />
                        <p>Loading community feed...</p>
                    </div>
                ) : (
                    <CommunityFeed
                        suggestions={suggestions}
                        currentAnonUserId={adminAnonUserId}
                        currentUserEmail={user?.email || 'Admin/Manager'}
                        organizationId={selectedOrgId}
                        // To allow refreshing from within CommunityFeed:
                        onRefresh={async () => {
                            try {
                                setLoading(true);
                                const allSuggestions = await SuggestionService.getAll();
                                const filtered = allSuggestions.filter(s => s.suggestionBoxId === selectedBoxId);
                                setSuggestions(filtered);
                            } catch (err) {
                                console.error(err);
                            } finally {
                                setLoading(false);
                            }
                        }}
                    />
                )}
            </div>
        </div>
    );
};

export default CommunityDashboard;
