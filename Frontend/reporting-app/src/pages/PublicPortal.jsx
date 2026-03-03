import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Shield, Send, ArrowRight, CheckCircle, ThumbsUp, MessageSquare, AlertTriangle, Loader2 } from 'lucide-react';
import { SuggestionService } from '../services/suggestionService';
import './PublicPortal.css';

const PublicPortal = () => {
    const [activeTab, setActiveTab] = useState('submit'); // 'submit' or 'browse'
    const [submissionType, setSubmissionType] = useState('suggestion');
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState(null);

    // Browse tab state
    const [suggestions, setSuggestions] = useState([]);
    const [browsing, setBrowsing] = useState(false);

    const [formData, setFormData] = useState({
        title: '',
        content: '',
        suggestionBoxId: '',
        anonymousUserId: ''
    });

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
        if (!formData.title || !formData.content) return;

        try {
            setIsSubmitting(true);
            setSubmitError(null);

            // Build the SuggestionCreationDTO
            const payload = {
                title: formData.title,
                description: formData.content,
                suggestionBoxId: formData.suggestionBoxId || '00000000-0000-0000-0000-000000000000',
                anonymousUserId: formData.anonymousUserId || '00000000-0000-0000-0000-000000000000',
                categoryIds: []
            };

            await SuggestionService.create(payload);
            setIsSubmitted(true);
        } catch (err) {
            console.error('Error submitting suggestion:', err);
            setSubmitError('Failed to submit. Please make sure the backend is running and try again.');
        } finally {
            setIsSubmitting(false);
        }
    };

    // Map numeric status to label
    const getStatusLabel = (status) => {
        const map = { 0: 'New', 1: 'In Progress', 2: 'Reviewing', 3: 'Resolved', 4: 'Closed' };
        return map[status] || 'Unknown';
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
                                <h2>Successfully Submitted securely!</h2>
                                <p>Your suggestion has been sent to the organization administrators.</p>
                                <div className="tracking-info">
                                    <p>To check for updates or reply securely, save this tracking phrase:</p>
                                    <div className="tracking-phrase">
                                        <code>purple-elephant-jumping-high</code>
                                    </div>
                                    <Link to="/track" className="btn btn-ghost mt-2" style={{ width: '100%', marginTop: '1rem' }}>
                                        Go to tracking page <ArrowRight size={16} className="ml-2" style={{ marginLeft: '0.5rem' }} />
                                    </Link>
                                </div>
                                <button className="btn btn-primary" onClick={() => {
                                    setFormData({ title: '', content: '', suggestionBoxId: '', anonymousUserId: '' });
                                    setIsSubmitted(false);
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

                                    <div className="form-group">
                                        <label>Suggestion Box ID</label>
                                        <input
                                            type="text"
                                            className="portal-input"
                                            placeholder="Enter the suggestion box GUID (optional)"
                                            value={formData.suggestionBoxId}
                                            onChange={(e) => setFormData({ ...formData, suggestionBoxId: e.target.value })}
                                        />
                                    </div>

                                    <div className="form-group">
                                        <label>Anonymous User ID</label>
                                        <input
                                            type="text"
                                            className="portal-input"
                                            placeholder="Enter your anonymous user GUID (optional)"
                                            value={formData.anonymousUserId}
                                            onChange={(e) => setFormData({ ...formData, anonymousUserId: e.target.value })}
                                        />
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
                                        disabled={isSubmitting}
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
                        ) : suggestions.length === 0 ? (
                            <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                                <p>No suggestions yet. Be the first to submit one!</p>
                            </div>
                        ) : (
                            <div className="suggestions-list">
                                {suggestions.map(suggestion => (
                                    <div key={suggestion.id} className="public-suggestion-card glass-panel">
                                        <div className="vote-column">
                                            <button className="vote-btn">
                                                <ThumbsUp size={20} />
                                            </button>
                                            <span className="vote-count">{getStatusLabel(suggestion.status)}</span>
                                        </div>
                                        <div className="suggestion-content">
                                            <h3 className="suggestion-title">{suggestion.title}</h3>
                                            <p className="suggestion-desc">{suggestion.description}</p>
                                            <div className="suggestion-meta">
                                                <span>
                                                    <MessageSquare size={14} />
                                                    {suggestion.categories?.length || 0} categories
                                                </span>
                                                <span>&bull;</span>
                                                <span>{suggestion.createdAt ? new Date(suggestion.createdAt).toLocaleDateString() : '—'}</span>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}
                        <button className="btn btn-ghost load-more-btn" onClick={fetchSuggestions}>Refresh</button>
                    </div>
                )}
            </div>
        </div>
    );
};

export default PublicPortal;
