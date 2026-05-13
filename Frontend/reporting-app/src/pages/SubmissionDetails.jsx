import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Send, Paperclip, Clock, Shield, AlertTriangle, Trash2, Loader2, Plus, Download, User } from 'lucide-react';
import { SuggestionService } from '../services/suggestionService';
import { ProblemService } from '../services/problemService';
import { AttachmentService } from '../services/attachmentService';
import { SuggestionCategoryService } from '../services/suggestionCategoryService';
import { SuggestionCommentService } from '../services/suggestionCommentService';
import { ProblemCommentService } from '../services/problemCommentService';
import { SystemNotificationService } from '../services/systemNotificationService';
import { useAuth } from '../context/AuthContext';
import './SubmissionDetails.css';

// Map numeric status to readable label
const statusMap = {
    0: 'New',
    1: 'In Progress',
    2: 'Reviewing',
    3: 'Resolved',
    4: 'Closed'
};

const SubmissionDetails = () => {
    const { submissionId } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [replyText, setReplyText] = useState('');
    const [comments, setComments] = useState([]);
    const [sendingReply, setSendingReply] = useState(false);

    // Real data state
    const [suggestion, setSuggestion] = useState(null);
    const [submissionType, setSubmissionType] = useState('suggestion'); // 'suggestion' | 'problem'
    const [attachments, setAttachments] = useState([]);
    const [availableCategories, setAvailableCategories] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Editable status
    const [status, setStatus] = useState('New');

    // UI State for categories
    const [isAddingCategory, setIsAddingCategory] = useState(false);
    const [selectedCategoryId, setSelectedCategoryId] = useState('');

    useEffect(() => {
        fetchSuggestionAndData();
        fetchCategories();
        fetchComments();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [submissionId]);

    const fetchComments = async () => {
        try {
            let data;
            if (submissionType === 'problem') {
                data = await ProblemCommentService.getByProblemId(submissionId);
            } else {
                data = await SuggestionCommentService.getBySuggestionId(submissionId);
            }
            setComments(Array.isArray(data) ? data : []);
        } catch (err) {
            console.error('Error fetching comments:', err);
            setComments([]);
        }
    };

    const fetchCategories = async () => {
        try {
            const cats = await SuggestionCategoryService.getAll();
            setAvailableCategories(cats);
        } catch (err) {
            console.error('Error fetching categories:', err);
        }
    };

    const fetchSuggestionAndData = async () => {
        try {
            setLoading(true);
            setError(null);

            // Try to fetch as suggestion first, fall back to problem
            let data = null;
            let type = 'suggestion';
            let attData = [];

            try {
                data = await SuggestionService.getById(submissionId);
                type = 'suggestion';
                attData = await AttachmentService.getBySuggestionId(submissionId).catch(() => []);
            } catch {
                // Not a suggestion, try problem
                try {
                    data = await ProblemService.getById(submissionId);
                    type = 'problem';
                    attData = await AttachmentService.getByProblemId(submissionId).catch(() => []);
                } catch {
                    throw new Error('Submission not found.');
                }
            }

            setSuggestion(data);
            setSubmissionType(type);
            setAttachments(attData);
            setStatus(typeof data.status === 'number' ? (statusMap[data.status] || 'New') : data.status);
        } catch (err) {
            console.error('Error fetching submission details:', err);
            setError('Failed to load submission details. Please check that the backend is running.');
        } finally {
            setLoading(false);
        }
    };

    const handleAddCategory = async () => {
        if (!selectedCategoryId) return;

        try {
            // Include existing category IDs plus the new one
            const currentCatIds = suggestion.categories?.map(c => c.id) || [];
            if (currentCatIds.includes(selectedCategoryId)) {
                setIsAddingCategory(false);
                setSelectedCategoryId('');
                return; // Already has it
            }

            const newCategoryIds = [...currentCatIds, selectedCategoryId];
            const updatePayload = {
                id: suggestion.id,
                title: suggestion.title,
                description: suggestion.description,
                status: suggestion.status,
                categoryIds: newCategoryIds
            };

            await SuggestionService.update(updatePayload);

            // Refresh to get updated object with full category info
            await fetchSuggestionAndData();

            setIsAddingCategory(false);
            setSelectedCategoryId('');
        } catch (err) {
            console.error('Error adding category:', err);
            alert('Failed to add category.');
        }
    };

    const handleDeleteSubmission = async () => {
        const label = submissionType === 'problem' ? 'problem' : 'suggestion';
        if (!window.confirm(`Are you sure you want to delete this ${label}?`)) return;
        try {
            if (submissionType === 'problem') {
                await ProblemService.delete(submissionId);
            } else {
                await SuggestionService.delete(submissionId);
            }
            navigate(-1);
        } catch (err) {
            console.error(`Error deleting ${label}:`, err);
            alert(`Failed to delete ${label}.`);
        }
    };

    const handleReplySubmit = async (e) => {
        e.preventDefault();
        if (!replyText.trim() || sendingReply) return;

        setSendingReply(true);
        try {
            const isProblem = submissionType === 'problem';
            let createdComment;

            if (isProblem) {
                createdComment = await ProblemCommentService.create({
                    problemId: submissionId,
                    commentText: replyText,
                    isAnonymous: false,
                    problemCommentAuthorId: user?.id || '00000000-0000-0000-0000-000000000000',
                    problemCommentId: null,
                    createdBy: user?.email || user?.name || 'Staff'
                });
            } else {
                createdComment = await SuggestionCommentService.create({
                    suggestionId: submissionId,
                    text: replyText,
                    isAnonymous: false,
                    suggestionCommentAuthorId: user?.id || '00000000-0000-0000-0000-000000000000',
                    suggestionCommentId: null,
                    createdBy: user?.email || user?.name || 'Staff'
                });
            }

            // 2. Create SystemNotification
            try {
                await SystemNotificationService.create({
                    text: replyText,
                    suggestionCommentId: !isProblem ? (createdComment.id || null) : null,
                    problemCommentId: isProblem ? (createdComment.id || null) : null,
                    organizationId: suggestion?.organizationId || null,
                    anonymousUserId: suggestion?.anonymousUserId || null
                });
            } catch (notifErr) {
                console.error('Failed to create system notification:', notifErr);
            }

            // 3. Refresh comments and clear input
            setReplyText('');
            await fetchComments();
        } catch (err) {
            console.error('Error sending reply:', err);
            alert('Failed to send reply. Please try again.');
        } finally {
            setSendingReply(false);
        }
    };

    if (loading) {
        return (
            <div className="submission-details-container animate-fade-in" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '400px' }}>
                <div style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
                    <Loader2 size={32} style={{ animation: 'spin 1s linear infinite', marginBottom: '1rem' }} />
                    <p>Loading submission details...</p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="submission-details-container animate-fade-in">
                <div className="back-link" onClick={() => navigate(-1)}>
                    <ArrowLeft size={16} /> Back to Box
                </div>
                <div className="glass-panel" style={{ padding: '2rem', textAlign: 'center', color: 'var(--danger)' }}>
                    <p>{error}</p>
                    <button className="btn btn-ghost" onClick={fetchSuggestionAndData} style={{ marginTop: '1rem' }}>
                        Retry
                    </button>
                </div>
            </div>
        );
    }

    // Derive display values from real data
    const isProblem = submissionType === 'problem';
    const typeLabel = isProblem ? 'Problem' : 'Suggestion';
    const title = suggestion?.title || `Untitled ${typeLabel}`;
    const description = suggestion?.description || 'No description provided.';
    const createdAt = suggestion?.createdAt ? new Date(suggestion.createdAt).toLocaleString() : '—';
    const categories = suggestion?.categories || [];
    const categoryNames = categories.length > 0
        ? categories.map(c => c.name).join(', ')
        : 'Uncategorized';

    return (
        <div className="submission-details-container animate-fade-in">
            <div className="back-link" onClick={() => navigate(-1)}>
                <ArrowLeft size={16} /> Back to Box
            </div>

            <div className="submission-header glass-panel">
                <div className="submission-header-top">
                    <div className="submission-meta-info">
                        <span className="meta-box-name">{typeLabel}</span>
                        <span className="meta-id">{submissionId}</span>
                    </div>
                    <div className="submission-actions">
                        <button className="btn btn-ghost icon-btn danger" title={`Delete ${typeLabel}`} onClick={handleDeleteSubmission}>
                            <Trash2 size={18} />
                        </button>
                    </div>
                </div>

                <h1 className="submission-title">{title}</h1>

                <div className="submission-tags">
                    <div className="tag-group">
                        <label>Status:</label>
                        <select
                            className="status-select"
                            value={status}
                            onChange={(e) => setStatus(e.target.value)}
                            style={{ color: status === 'Resolved' ? 'var(--success)' : status === 'In Progress' ? 'var(--warning)' : 'var(--accent-primary)' }}
                        >
                            <option value="New">New</option>
                            <option value="In Progress">In Progress</option>
                            <option value="Reviewing">Reviewing</option>
                            <option value="Resolved">Resolved</option>
                            <option value="Closed">Closed</option>
                        </select>
                    </div>

                    {!isProblem && (
                        <div className="tag-group">
                            <label>Category:</label>
                            <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', alignItems: 'center' }}>
                                {categories.length > 0 ? (
                                    categories.map(c => (
                                        <span key={c.id} className="readonly-tag">{c.name || c.title}</span>
                                    ))
                                ) : (
                                    <span className="readonly-tag" style={{ background: 'transparent', border: '1px dashed var(--border-subtle)' }}>None</span>
                                )}

                                {isAddingCategory ? (
                                    <div style={{ display: 'flex', gap: '0.25rem', alignItems: 'center' }}>
                                        <select
                                            className="portal-input"
                                            style={{ padding: '0.25rem 0.5rem', fontSize: '0.8rem', minWidth: '120px', height: 'auto' }}
                                            value={selectedCategoryId}
                                            onChange={(e) => setSelectedCategoryId(e.target.value)}
                                            autoFocus
                                        >
                                            <option value="">Select...</option>
                                            {availableCategories
                                                .filter(ac => !categories.find(c => c.id === ac.id))
                                                .map(ac => (
                                                    <option key={ac.id} value={ac.id}>{ac.title}</option>
                                                ))}
                                        </select>
                                        <button
                                            className="btn btn-primary"
                                            style={{ padding: '0.25rem 0.5rem', fontSize: '0.8rem', height: 'auto' }}
                                            onClick={handleAddCategory}
                                        >
                                            Add
                                        </button>
                                        <button
                                            className="btn btn-ghost"
                                            style={{ padding: '0.25rem 0.5rem', fontSize: '0.8rem', height: 'auto' }}
                                            onClick={() => { setIsAddingCategory(false); setSelectedCategoryId(''); }}
                                        >
                                            Cancel
                                        </button>
                                    </div>
                                ) : (
                                    <button
                                        className="btn btn-ghost icon-btn"
                                        style={{ padding: '0.25rem' }}
                                        onClick={() => setIsAddingCategory(true)}
                                        title="Add Category"
                                    >
                                        <Plus size={16} />
                                    </button>
                                )}
                            </div>
                        </div>
                    )}

                    <div className="tag-group">
                        <label>{isProblem ? 'Problem Box ID:' : 'Suggestion Box ID:'}</label>
                        <span className="readonly-tag" style={{ fontSize: '0.8rem' }}>{isProblem ? (suggestion?.problemBoxId || '—') : (suggestion?.suggestionBoxId || '—')}</span>
                    </div>
                </div>
            </div>

            <div className="submission-content-wrapper">
                {/* Main Content Area */}
                <div className="submission-main">
                    {/* Original Post */}
                    <div className="message-bubble op glass-panel">
                        <div className="message-header">
                            <div className="message-author">
                                <div className="author-avatar anonymous"><Shield size={14} /></div>
                                <span className="author-name">Anonymous</span>
                                <span className="author-badge">Original Poster</span>
                            </div>
                            <span className="message-time"><Clock size={12} /> {createdAt}</span>
                        </div>
                        <div className="message-body">
                            <p>{description}</p>

                            {/* Attachments Section */}
                            {attachments.length > 0 && (
                                <div className="submission-attachments" style={{ marginTop: '1.5rem', paddingTop: '1rem', borderTop: '1px solid var(--border-subtle)' }}>
                                    <h4 style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                        <Paperclip size={14} /> Attached Evidence
                                    </h4>
                                    <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                                        {attachments.map(att => (
                                            <a
                                                key={att.id}
                                                href={att.url}
                                                download={att.fileName}
                                                target="_blank"
                                                rel="noreferrer"
                                                style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '0.5rem 0.75rem', background: 'rgba(0,0,0,0.2)', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', fontSize: '0.875rem', textDecoration: 'none', color: 'inherit', transition: 'background 0.2s' }}
                                                onMouseOver={(e) => e.currentTarget.style.background = 'rgba(255,255,255,0.05)'}
                                                onMouseOut={(e) => e.currentTarget.style.background = 'rgba(0,0,0,0.2)'}
                                            >
                                                <Download size={14} style={{ color: 'var(--accent-primary)' }} />
                                                <span style={{ color: 'var(--text-primary)' }}>{att.fileName}</span>
                                            </a>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>

                    <div className="thread-divider">
                        <span>Communication Thread</span>
                    </div>

                    {/* Thread messages */}
                    <div className="thread-container">
                        {comments.length === 0 ? (
                            <div style={{ textAlign: 'center', padding: '1.5rem', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
                                No replies yet.
                            </div>
                        ) : (
                            comments.map((comment) => (
                                <div key={comment.id} className="message-bubble admin glass-panel">
                                    <div className="message-header">
                                        <div className="message-author">
                                            <div className="author-avatar" style={{ background: 'rgba(168, 85, 247, 0.2)', color: '#a855f7' }}>
                                                <User size={14} />
                                            </div>
                                            <span className="author-name">{comment.createdBy || (user?.role ? user.role.charAt(0).toUpperCase() + user.role.slice(1) : 'Staff')}</span>
                                            <span className="author-badge">Reply</span>
                                        </div>
                                        <span className="message-time">
                                            <Clock size={12} />{' '}
                                            {comment.createdAt ? new Date(comment.createdAt).toLocaleString() : '—'}
                                        </span>
                                    </div>
                                    <div className="message-body">
                                        <p>{comment.commentText || comment.text}</p>
                                    </div>
                                </div>
                            ))
                        )}
                    </div>

                    {/* Reply Box */}
                    <div className="reply-box glass-panel">
                        <div className="reply-header">
                            Reply as <strong>{user?.role ? user.role.charAt(0).toUpperCase() + user.role.slice(1) : 'User'}</strong>
                        </div>
                        <form onSubmit={handleReplySubmit}>
                            <textarea
                                className="reply-textarea"
                                placeholder="Type your response here. The author will be notified..."
                                value={replyText}
                                onChange={(e) => setReplyText(e.target.value)}
                            />
                            <div className="reply-actions">
                                <button type="button" className="btn btn-ghost icon-btn" title="Attach file">
                                    <Paperclip size={18} />
                                </button>
                                <button type="submit" className="btn btn-primary" disabled={!replyText.trim() || sendingReply}>
                                    {sendingReply ? <Loader2 size={16} style={{ animation: 'spin 1s linear infinite' }} /> : <Send size={16} />}
                                    {sendingReply ? ' Sending...' : ' Send Reply'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>

                {/* Sidebar Context Panel */}
                <div className="submission-sidebar">
                    {/* Suggestion Info Card */}
                    <div className="context-card glass-panel">
                        <h3>{typeLabel} Info</h3>
                        <div style={{ fontSize: '0.85rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                            <div>
                                <span style={{ color: 'var(--text-muted)' }}>ID: </span>
                                <code style={{ color: 'var(--text-secondary)', fontSize: '0.8rem' }}>{suggestion?.id}</code>
                            </div>
                            <div>
                                <span style={{ color: 'var(--text-muted)' }}>Created: </span>
                                <span>{createdAt}</span>
                            </div>
                            {!isProblem && (
                                <div>
                                    <span style={{ color: 'var(--text-muted)' }}>Categories: </span>
                                    <span>{categoryNames}</span>
                                </div>
                            )}
                            <div>
                                <span style={{ color: 'var(--text-muted)' }}>{isProblem ? 'Problem Box: ' : 'Suggestion Box: '}</span>
                                <code style={{ color: 'var(--text-secondary)', fontSize: '0.8rem' }}>{isProblem ? (suggestion?.problemBoxId || '—') : (suggestion?.suggestionBoxId || '—')}</code>
                            </div>
                        </div>
                    </div>

                    <div className="context-card glass-panel alert-card">
                        <h3><AlertTriangle size={18} /> Reporter Privacy</h3>
                        <p className="help-text">
                            This report was submitted securely. No identifiable metadata, IP addresses, or location data was collected. Do not ask the reporter for personally identifiable information unless strictly necessary for resolution.
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default SubmissionDetails;
