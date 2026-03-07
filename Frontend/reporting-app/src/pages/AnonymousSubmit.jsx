import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Shield, Send, ArrowRight, CheckCircle, AlertTriangle, Loader2, LogOut, User } from 'lucide-react';
import { SuggestionService } from '../services/suggestionService';
import { SuggestionBoxService } from '../services/suggestionBoxService';
import { ProblemService } from '../services/problemService';
import { ProblemBoxService } from '../services/problemBoxService';
import './PublicPortal.css';

const statusMap = { 0: 'New', 1: 'In Progress', 2: 'Reviewing', 3: 'Resolved', 4: 'Closed' };

const AnonymousSubmit = () => {
    const navigate = useNavigate();
    const [anonUser, setAnonUser] = useState(null);
    const [submissionType, setSubmissionType] = useState('suggestion');
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState(null);
    const [createdItem, setCreatedItem] = useState(null);

    // Box data
    const [suggestionBoxes, setSuggestionBoxes] = useState([]);
    const [problemBoxes, setProblemBoxes] = useState([]);

    const [formData, setFormData] = useState({
        title: '',
        content: '',
        boxId: ''
    });

    // Check if user is logged in
    useEffect(() => {
        const stored = localStorage.getItem('anonymousUser');
        if (!stored) {
            navigate('/anonymous/login');
            return;
        }
        try {
            setAnonUser(JSON.parse(stored));
        } catch {
            navigate('/anonymous/login');
        }
    }, [navigate]);

    // Fetch boxes on mount
    useEffect(() => {
        const fetchBoxes = async () => {
            try {
                const [sugBoxes, probBoxes] = await Promise.all([
                    SuggestionBoxService.getAll(),
                    ProblemBoxService.getAll()
                ]);
                setSuggestionBoxes(sugBoxes);
                setProblemBoxes(probBoxes);

                // Pre-select first box
                if (sugBoxes.length > 0) {
                    setFormData(prev => ({ ...prev, boxId: sugBoxes[0].id }));
                }
            } catch (err) {
                console.error('Error fetching boxes:', err);
            }
        };
        fetchBoxes();
    }, []);

    // Update selected box when switching submission type
    useEffect(() => {
        if (submissionType === 'suggestion' && suggestionBoxes.length > 0) {
            setFormData(prev => ({ ...prev, boxId: suggestionBoxes[0].id }));
        } else if (submissionType === 'problem' && problemBoxes.length > 0) {
            setFormData(prev => ({ ...prev, boxId: problemBoxes[0].id }));
        }
    }, [submissionType, suggestionBoxes, problemBoxes]);

    const handleLogout = () => {
        localStorage.removeItem('anonymousUser');
        navigate('/anonymous/login');
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!formData.title || !formData.content) return;

        try {
            setIsSubmitting(true);
            setSubmitError(null);

            if (submissionType === 'suggestion') {
                const payload = {
                    title: formData.title,
                    description: formData.content,
                    suggestionBoxId: formData.boxId || '00000000-0000-0000-0000-000000000000',
                    anonymousUserId: anonUser?.id || '00000000-0000-0000-0000-000000000000',
                    categoryIds: []
                };
                const result = await SuggestionService.create(payload);
                setCreatedItem(result);
            } else {
                const payload = {
                    title: formData.title,
                    description: formData.content,
                    problemBoxId: formData.boxId || '00000000-0000-0000-0000-000000000000',
                    anonymousUserId: anonUser?.id || '00000000-0000-0000-0000-000000000000',
                    categoryIds: []
                };
                const result = await ProblemService.create(payload);
                setCreatedItem(result);
            }

            setIsSubmitted(true);
        } catch (err) {
            console.error('Error submitting:', err);
            setSubmitError('Failed to submit. Please make sure the backend is running and try again.');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleSubmitAnother = () => {
        setFormData(prev => ({ ...prev, title: '', content: '' }));
        setIsSubmitted(false);
        setCreatedItem(null);
        setSubmitError(null);
    };

    const currentBoxes = submissionType === 'suggestion' ? suggestionBoxes : problemBoxes;

    if (!anonUser) return null;

    return (
        <div className="portal-container animate-fade-in">
            <div className="portal-header">
                <div className="portal-header-content glass-panel">
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                            <div style={{
                                width: 40, height: 40, borderRadius: '50%',
                                background: 'linear-gradient(135deg, #6366f1, #a855f7)',
                                display: 'flex', alignItems: 'center', justifyContent: 'center'
                            }}>
                                <User size={20} color="white" />
                            </div>
                            <div>
                                <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>Logged in as</span>
                                <div style={{ color: 'var(--text-primary)', fontWeight: 600, fontSize: '0.95rem' }}>
                                    {anonUser.username || 'Anonymous User'}
                                </div>
                            </div>
                        </div>
                        <button
                            onClick={handleLogout}
                            className="btn btn-ghost"
                            style={{ fontSize: '0.85rem', gap: '0.4rem' }}
                        >
                            <LogOut size={16} /> Log Out
                        </button>
                    </div>

                    <h1>Submit Feedback</h1>
                    <p>Submit a suggestion or report a problem anonymously.</p>
                </div>
            </div>

            <div className="portal-main-content">
                {isSubmitted ? (
                    <div className="submit-section fade-in">
                        <div className="success-state glass-panel">
                            <div className="success-icon">
                                <CheckCircle size={48} />
                            </div>
                            <h2>Successfully Submitted!</h2>
                            <p>Your {submissionType} has been sent to the organization administrators.</p>
                            {createdItem && (
                                <div className="tracking-info">
                                    <p>Your submission details:</p>
                                    <div style={{
                                        textAlign: 'left', padding: '1rem',
                                        background: 'rgba(0,0,0,0.2)', borderRadius: 'var(--radius-sm)',
                                        marginBottom: '1rem'
                                    }}>
                                        <p><strong>ID:</strong> <code>{createdItem.id}</code></p>
                                        <p><strong>Title:</strong> {createdItem.title}</p>
                                        <p><strong>Status:</strong> {statusMap[createdItem.status] || createdItem.status}</p>
                                    </div>
                                </div>
                            )}
                            <button className="btn btn-primary" onClick={handleSubmitAnother}>
                                Submit Another
                            </button>
                        </div>
                    </div>
                ) : (
                    <div className="submit-section fade-in">
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
                                    <label>
                                        {submissionType === 'suggestion' ? 'Suggestion Box' : 'Problem Box'}{' '}
                                        <span className="required">*</span>
                                    </label>
                                    {currentBoxes.length > 0 ? (
                                        <select
                                            className="portal-input"
                                            value={formData.boxId}
                                            onChange={(e) => setFormData({ ...formData, boxId: e.target.value })}
                                        >
                                            {currentBoxes.map(box => (
                                                <option key={box.id} value={box.id}>
                                                    {box.name} {box.description ? `— ${box.description}` : ''}
                                                </option>
                                            ))}
                                        </select>
                                    ) : (
                                        <input
                                            type="text"
                                            className="portal-input"
                                            placeholder={`Enter ${submissionType === 'suggestion' ? 'Suggestion' : 'Problem'} Box GUID`}
                                            value={formData.boxId}
                                            onChange={(e) => setFormData({ ...formData, boxId: e.target.value })}
                                        />
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
                                    <div style={{
                                        color: 'var(--danger)', padding: '0.75rem',
                                        background: 'rgba(255,0,0,0.1)', borderRadius: 'var(--radius-sm)',
                                        marginBottom: '1rem'
                                    }}>
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
                    </div>
                )}
            </div>
        </div>
    );
};

export default AnonymousSubmit;
