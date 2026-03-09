import React, { useState, useEffect } from 'react';
import { Shield, Send, ArrowRight, CheckCircle, AlertTriangle, Loader2, Paperclip } from 'lucide-react';
import { jwtDecode } from 'jwt-decode';
import { SuggestionService } from '../services/suggestionService';
import { SuggestionBoxService } from '../services/suggestionBoxService';
import { ProblemService } from '../services/problemService';
import { ProblemBoxService } from '../services/problemBoxService';
import { OrganizationService } from '../services/organizationService';
import { AttachmentService } from '../services/attachmentService';
import { SystemNotificationService } from '../services/systemNotificationService';
import CommunityFeed from '../components/Community/CommunityFeed';
import './PublicPortal.css';

// Map numeric status to label
const statusMap = { 0: 'New', 1: 'In Progress', 2: 'Reviewing', 3: 'Resolved', 4: 'Closed' };

const PublicPortal = () => {
    const [activeTab, setActiveTab] = useState('submit');
    const [submissionType, setSubmissionType] = useState('suggestion');
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState(null);
    const [createdSuggestion, setCreatedSuggestion] = useState(null);

    // Browse tab state
    const [suggestions, setSuggestions] = useState([]);
    const [browsing, setBrowsing] = useState(false);

    // Organizations and boxes
    const [organizations, setOrganizations] = useState([]);
    const [suggestionBoxes, setSuggestionBoxes] = useState([]);
    const [problemBoxes, setProblemBoxes] = useState([]);
    const [loadingBoxes, setLoadingBoxes] = useState(false);

    const [formData, setFormData] = useState({
        title: '',
        content: '',
        organizationId: '',
        suggestionBoxId: ''
    });

    // Optional attachment
    const [attachment, setAttachment] = useState(null);

    // Fetch organizations on mount
    useEffect(() => {
        const fetchOrganizations = async () => {
            try {
                const orgs = await OrganizationService.getAll();
                setOrganizations(orgs);
            } catch (err) {
                console.error('Error fetching organizations:', err);
            }
        };
        fetchOrganizations();
    }, []);

    // When organization changes, fetch suggestion boxes and problem boxes for that org
    useEffect(() => {
        if (!formData.organizationId) {
            setSuggestionBoxes([]);
            setProblemBoxes([]);
            setFormData(prev => ({ ...prev, suggestionBoxId: '' }));
            return;
        }

        const fetchBoxesForOrg = async () => {
            try {
                setLoadingBoxes(true);
                const [sBoxes, pBoxes] = await Promise.all([
                    SuggestionBoxService.getByOrganizationId(formData.organizationId),
                    ProblemBoxService.getByOrganizationId(formData.organizationId)
                ]);

                setSuggestionBoxes(sBoxes);
                setProblemBoxes(pBoxes);

                // Pre-select first box based on current type
                const currentBoxes = submissionType === 'suggestion' ? sBoxes : pBoxes;
                if (currentBoxes.length > 0) {
                    setFormData(prev => ({ ...prev, suggestionBoxId: currentBoxes[0].id }));
                } else {
                    setFormData(prev => ({ ...prev, suggestionBoxId: '' }));
                }
            } catch (err) {
                console.error('Error fetching boxes for organization:', err);
                setSuggestionBoxes([]);
                setProblemBoxes([]);
                setFormData(prev => ({ ...prev, suggestionBoxId: '' }));
            } finally {
                setLoadingBoxes(false);
            }
        };
        fetchBoxesForOrg();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [formData.organizationId, submissionType]);

    // Fetch suggestions when browse tab is active
    useEffect(() => {
        if (activeTab === 'browse') {
            fetchSuggestions();
        }
    }, [activeTab]);

    const fetchSuggestions = async () => {
        try {
            setBrowsing(true);
            const data = await SuggestionService.getAll();
            setSuggestions(data);
        } catch (err) {
            console.error('Error fetching suggestions:', err);
        } finally {
            setBrowsing(false);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!formData.title || !formData.content || !formData.suggestionBoxId) return;

        try {
            setIsSubmitting(true);
            setSubmitError(null);

            // Read auth token and decode anonymous user ID
            const authToken = localStorage.getItem('authToken');
            let anonymousUserId = '00000000-0000-0000-0000-000000000000';
            if (authToken) {
                try {
                    const decoded = jwtDecode(authToken);
                    anonymousUserId = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || anonymousUserId;
                } catch (decodeErr) {
                    console.error('Failed to decode auth token:', decodeErr);
                }
            }

            // Build the appropriate DTO based on submission type
            let result;
            if (submissionType === 'suggestion') {
                const payload = {
                    title: formData.title,
                    description: formData.content,
                    suggestionBoxId: formData.suggestionBoxId,
                    anonymousUserId: anonymousUserId,
                    categoryIds: []
                };
                result = await SuggestionService.create(payload, authToken);
            } else {
                const payload = {
                    title: formData.title,
                    description: formData.content,
                    problemBoxId: formData.suggestionBoxId,
                    anonymousUserId: anonymousUserId,
                    categoryIds: []
                };
                result = await ProblemService.create(payload, authToken);
            }

            // If there's an attachment, upload it
            if (attachment) {
                try {
                    const base64String = await new Promise((resolve, reject) => {
                        const reader = new FileReader();
                        reader.readAsDataURL(attachment);
                        reader.onload = () => resolve(reader.result);
                        reader.onerror = error => reject(error);
                    });

                    await AttachmentService.create({
                        fileName: attachment.name,
                        fileType: attachment.type || 'application/octet-stream',
                        url: base64String, // the Base64 data URL
                        suggestionId: submissionType === 'suggestion' ? result.id : null,
                        problemId: submissionType === 'problem' ? result.id : null
                    });
                } catch (attachErr) {
                    console.error('Error uploading attachment:', attachErr);
                    // Non-blocking, the suggestion was still created successfully
                }
            }

            // Create SystemNotification for the new submission
            try {
                await SystemNotificationService.create({
                    text: formData.content,
                    organizationId: formData.organizationId || null,
                    anonymousUserId: anonymousUserId || null,
                    suggestionCommentId: null,
                    problemCommentId: null
                });
            } catch (notifErr) {
                console.error('Failed to create system notification:', notifErr);
            }

            setCreatedSuggestion(result);
            setIsSubmitted(true);
            setAttachment(null);
        } catch (err) {
            console.error('Error submitting suggestion:', err);
            setSubmitError('Failed to submit. Please make sure the backend is running and try again.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="portal-container animate-fade-in">

            <div className="portal-header">
                <div className="portal-header-content glass-panel">
                    <div className="portal-org-meta">
                        <span className="portal-org-name">Anonymous Reporting</span>
                        <span className="portal-box-name">Employee Feedback Hub</span>
                    </div>
                    <h1>Speak up, safely.</h1>
                    <p>Your voice matters. Submit suggestions or report issues completely anonymously.</p>

                    <div className="portal-tabs">
                        <button
                            className={`portal-tab ${activeTab === 'submit' ? 'active' : ''}`}
                            onClick={() => setActiveTab('submit')}
                        >
                            Submit Feedback
                        </button>
                        <button
                            className={`portal-tab ${activeTab === 'browse' ? 'active' : ''}`}
                            onClick={() => setActiveTab('browse')}
                        >
                            Community Board
                        </button>
                    </div>
                </div>
            </div>

            <div className="portal-main-content">
                {activeTab === 'submit' ? (
                    <div className="submit-section fade-in">
                        {isSubmitted ? (
                            <div className="success-state glass-panel">
                                <div className="success-icon">
                                    <CheckCircle size={48} />
                                </div>
                                <h2>Successfully Submitted!</h2>
                                <p>Your {submissionType === 'problem' ? 'problem' : 'suggestion'} has been sent to the organization administrators.</p>
                                {createdSuggestion && (
                                    <div className="tracking-info">
                                        <p>Your {submissionType === 'problem' ? 'problem' : 'suggestion'} details:</p>
                                        <div style={{ textAlign: 'left', padding: '1rem', background: 'rgba(0,0,0,0.2)', borderRadius: 'var(--radius-sm)', marginBottom: '1rem' }}>
                                            <p><strong>ID:</strong> <code>{createdSuggestion.id}</code></p>
                                            <p><strong>Title:</strong> {createdSuggestion.title}</p>
                                            <p><strong>Description:</strong> {createdSuggestion.description}</p>
                                            <p><strong>Status:</strong> {statusMap[createdSuggestion.status] || createdSuggestion.status}</p>
                                        </div>
                                    </div>
                                )}
                                <button className="btn btn-primary" onClick={() => {
                                    setFormData(prev => ({ title: '', content: '', organizationId: prev.organizationId, suggestionBoxId: prev.suggestionBoxId }));
                                    setIsSubmitted(false);
                                    setCreatedSuggestion(null);
                                }}>
                                    Submit Another
                                </button>
                            </div>
                        ) : (
                            <div className="submission-form-container glass-panel">
                                <div className="form-type-selector">
                                    <button
                                        type="button"
                                        className={`type-btn ${submissionType === 'suggestion' ? 'active' : ''}`}
                                        onClick={() => setSubmissionType('suggestion')}
                                    >
                                        💡 Suggestion
                                    </button>
                                    <button
                                        type="button"
                                        className={`type-btn ${submissionType === 'problem' ? 'active' : ''}`}
                                        onClick={() => setSubmissionType('problem')}
                                    >
                                        ⚠️ Report a Problem
                                    </button>
                                </div>

                                <form className="public-submit-form" onSubmit={handleSubmit}>
                                    {/* Step 1: Select Organization */}
                                    <div className="form-group">
                                        <label>Organization <span className="required">*</span></label>
                                        {organizations.length > 0 ? (
                                            <select
                                                className="portal-input"
                                                value={formData.organizationId}
                                                onChange={(e) => setFormData({ ...formData, organizationId: e.target.value, suggestionBoxId: '' })}
                                            >
                                                <option value="">— Select an organization —</option>
                                                {organizations.map(org => (
                                                    <option key={org.id} value={org.id}>
                                                        {org.name}
                                                    </option>
                                                ))}
                                            </select>
                                        ) : (
                                            <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>Loading organizations...</p>
                                        )}
                                    </div>

                                    {/* Step 2: Select Box (only after organization is selected) */}
                                    <div className="form-group">
                                        <label>{submissionType === 'suggestion' ? 'Suggestion Box' : 'Problem Box'} <span className="required">*</span></label>
                                        {!formData.organizationId ? (
                                            <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>Please select an organization first.</p>
                                        ) : loadingBoxes ? (
                                            <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>Loading boxes...</p>
                                        ) : (submissionType === 'suggestion' ? suggestionBoxes : problemBoxes).length > 0 ? (
                                            <select
                                                className="portal-input"
                                                value={formData.suggestionBoxId} // We reuse this field for problemBoxId as well
                                                onChange={(e) => setFormData({ ...formData, suggestionBoxId: e.target.value })}
                                            >
                                                {(submissionType === 'suggestion' ? suggestionBoxes : problemBoxes).map(box => (
                                                    <option key={box.id} value={box.id}>
                                                        {box.name} {box.description ? `— ${box.description}` : ''}
                                                    </option>
                                                ))}
                                            </select>
                                        ) : (
                                            <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>No {submissionType} boxes found for this organization.</p>
                                        )}
                                    </div>

                                    <div className="form-group">
                                        <label>Title <span className="required">*</span></label>
                                        <input
                                            type="text"
                                            className="portal-input"
                                            placeholder="Briefly summarize your feedback"
                                            value={formData.title}
                                            onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                                            required
                                        />
                                    </div>

                                    <div className="form-group">
                                        <label>Details <span className="required">*</span></label>
                                        <textarea
                                            className="portal-input textarea"
                                            rows="6"
                                            placeholder="Provide as much context as possible..."
                                            value={formData.content}
                                            onChange={(e) => setFormData({ ...formData, content: e.target.value })}
                                            required
                                        />
                                    </div>

                                    {/* Optional Attachment */}
                                    <div className="form-group">
                                        <label><Paperclip size={14} style={{ marginRight: '0.25rem', verticalAlign: 'middle' }} /> Attachment (Optional)</label>
                                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                                            <input
                                                type="file"
                                                className="portal-input"
                                                style={{ padding: '0.5rem' }}
                                                onChange={(e) => setAttachment(e.target.files[0] || null)}
                                            />
                                        </div>
                                        {attachment && (
                                            <p style={{ color: 'var(--text-muted)', fontSize: '0.8rem', marginTop: '0.35rem' }}>
                                                Selected: {attachment.name} ({(attachment.size / 1024).toFixed(1)} KB)
                                            </p>
                                        )}
                                    </div>

                                    <div className="form-info-card privacy-card">
                                        <Shield size={20} className="text-success" />
                                        <div>
                                            <strong>100% Anonymous Delivery</strong>
                                            <p>We do not collect IP addresses or device data. The organization will only see the text you provide above.</p>
                                        </div>
                                    </div>

                                    {submissionType === 'problem' && (
                                        <div className="form-info-card alert-card">
                                            <AlertTriangle size={20} className="text-warning" />
                                            <div>
                                                <strong>Whistleblower Protection</strong>
                                                <p>If you are reporting severe misconduct, please ensure you omit self-identifying details from your description.</p>
                                            </div>
                                        </div>
                                    )}

                                    {submitError && (
                                        <div style={{ color: 'var(--danger)', padding: '0.75rem', background: 'rgba(255,0,0,0.1)', borderRadius: 'var(--radius-sm)', marginBottom: '1rem' }}>
                                            {submitError}
                                        </div>
                                    )}

                                    <button
                                        type="submit"
                                        className="btn btn-primary btn-lg submit-btn bounce-hover"
                                        disabled={isSubmitting || !formData.organizationId || !formData.suggestionBoxId}
                                    >
                                        {isSubmitting ? (
                                            <><Loader2 size={18} style={{ animation: 'spin 1s linear infinite' }} /> Submitting...</>
                                        ) : (
                                            <><Send size={18} /> Submit Securely <ArrowRight size={18} /></>
                                        )}
                                    </button>
                                </form>
                            </div>
                        )}
                    </div>
                ) : (
                    <div className="browse-section fade-in">
                        <div className="board-filters">
                            <h3>All Suggestions</h3>
                            <div className="filter-sort">
                                <select className="portal-select">
                                    <option>Most Recent</option>
                                    <option>Oldest First</option>
                                </select>
                            </div>
                        </div>

                        {browsing ? (
                            <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                                <Loader2 size={32} style={{ animation: 'spin 1s linear infinite', marginBottom: '1rem' }} />
                                <p>Loading suggestions...</p>
                            </div>
                        ) : (
                            <CommunityFeed
                                suggestions={suggestions}
                                currentAnonUserId={(() => {
                                    const token = localStorage.getItem('authToken');
                                    let id = '00000000-0000-0000-0000-000000000000';
                                    if (token) {
                                        try {
                                            const dec = jwtDecode(token);
                                            id = dec['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || id;
                                        } catch (e) { }
                                    }
                                    return id;
                                })()}
                                currentUserEmail={(() => {
                                    // Use the same ID as their unique email/key for local storage liking isolation
                                    const token = localStorage.getItem('authToken');
                                    let key = 'AnonymousVisitor';
                                    if (token) {
                                        try {
                                            const dec = jwtDecode(token);
                                            key = dec['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || key;
                                        } catch (e) { }
                                    }
                                    return key;
                                })()}
                                onRefresh={fetchSuggestions}
                            />
                        )}
                        <button className="btn btn-ghost load-more-btn" onClick={fetchSuggestions} style={{ width: '100%', marginTop: '1rem' }}>Refresh</button>
                    </div>
                )}
            </div>
        </div>
    );
};

export default PublicPortal;
