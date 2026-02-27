import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Send, Paperclip, Clock, Shield, AlertTriangle, Trash2 } from 'lucide-react';
import './SubmissionDetails.css';

// Mock data
const mockSubmission = {
    id: 'SUB-101',
    boxName: 'Facilities & Maintenance',
    title: 'Coffee machine in breakroom is broken',
    content: 'The coffee machine on the 2nd floor has been leaking water since Tuesday. It needs maintenance immediately as it is creating a slip hazard near the power outlets.',
    author: 'Anonymous',
    date: 'Oct 24, 2023, 10:30 AM',
    status: 'In Progress',
    priority: 'High',
    category: 'Maintenance',
    thread: [
        { id: 1, sender: 'author', role: 'Anonymous', text: 'Please send someone soon, the puddle is getting bigger.', time: 'Oct 24, 2023, 11:15 AM' },
        { id: 2, sender: 'admin', role: 'Jane Smith (Admin)', text: 'Thank you for reporting this. I have contacted building maintenance, they should arrive within the hour.', time: 'Oct 24, 2023, 11:45 AM' }
    ],
    type: 'Problem', // Problem or Suggestion
    attachments: [
        { id: 'att-1', name: 'puddle_photo.jpg', size: '2.4 MB' },
        { id: 'att-2', name: 'machine_serial.png', size: '1.1 MB' }
    ],
    upvotes: 0 // Only applicable for suggestions
};

const SubmissionDetails = () => {
    const { submissionId } = useParams();
    const navigate = useNavigate();
    const [replyText, setReplyText] = useState('');

    // States for actions
    const [priority, setPriority] = useState(mockSubmission.priority);
    const [status, setStatus] = useState(mockSubmission.status);

    const handleReplySubmit = (e) => {
        e.preventDefault();
        if (!replyText.trim()) return;
        console.log('Sent reply:', replyText);
        setReplyText('');
    };

    return (
        <div className="submission-details-container animate-fade-in">
            <div className="back-link" onClick={() => navigate(-1)}>
                <ArrowLeft size={16} /> Back to Box
            </div>

            <div className="submission-header glass-panel">
                <div className="submission-header-top">
                    <div className="submission-meta-info">
                        <span className="meta-box-name">{mockSubmission.boxName}</span>
                        <span className="meta-id">{submissionId || mockSubmission.id}</span>
                    </div>
                    <div className="submission-actions">
                        <button className="btn btn-ghost icon-btn danger" title="Delete Submission">
                            <Trash2 size={18} />
                        </button>
                    </div>
                </div>

                <h1 className="submission-title">{mockSubmission.title}</h1>

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
                            <option value="Resolved">Resolved</option>
                        </select>
                    </div>

                    <div className="tag-group">
                        <label>Priority:</label>
                        <select
                            className="status-select"
                            value={priority}
                            onChange={(e) => setPriority(e.target.value)}
                            style={{ color: priority === 'High' ? 'var(--danger)' : priority === 'Medium' ? 'var(--warning)' : 'var(--success)' }}
                        >
                            <option value="High">High</option>
                            <option value="Medium">Medium</option>
                            <option value="Low">Low</option>
                        </select>
                    </div>

                    <div className="tag-group">
                        <label>Category:</label>
                        <span className="readonly-tag">{mockSubmission.category || 'Uncategorized'}</span>
                    </div>

                    {mockSubmission.type === 'Suggestion' && (
                        <div className="tag-group">
                            <label>Community Votes:</label>
                            <span className="readonly-tag" style={{ color: 'var(--accent-primary)', fontWeight: 'bold' }}>
                                👍 {mockSubmission.upvotes}
                            </span>
                        </div>
                    )}
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
                                <span className="author-name">{mockSubmission.author}</span>
                                <span className="author-badge">Original Poster</span>
                            </div>
                            <span className="message-time"><Clock size={12} /> {mockSubmission.date}</span>
                        </div>
                        <div className="message-body">
                            <p>{mockSubmission.content}</p>

                            {/* Render explicit Attachments (aggregate rule for Problems) */}
                            {mockSubmission.type === 'Problem' && mockSubmission.attachments?.length > 0 && (
                                <div className="submission-attachments" style={{ marginTop: '1.5rem', paddingTop: '1rem', borderTop: '1px solid var(--border-subtle)' }}>
                                    <h4 style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                        <Paperclip size={14} /> Attached Evidence
                                    </h4>
                                    <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                                        {mockSubmission.attachments.map(att => (
                                            <div key={att.id} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '0.5rem 0.75rem', background: 'rgba(0,0,0,0.2)', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)', fontSize: '0.875rem' }}>
                                                <span style={{ color: 'var(--text-primary)' }}>{att.name}</span>
                                                <span style={{ color: 'var(--text-muted)', fontSize: '0.75rem' }}>({att.size})</span>
                                            </div>
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
                        {mockSubmission.thread.map((msg) => (
                            <div key={msg.id} className={`message-bubble ${msg.sender} glass-panel`}>
                                <div className="message-header">
                                    <div className="message-author">
                                        <span className="author-name">{msg.role}</span>
                                    </div>
                                    <span className="message-time">{msg.time}</span>
                                </div>
                                <div className="message-body">
                                    <p>{msg.text}</p>
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Reply Box */}
                    <div className="reply-box glass-panel">
                        <div className="reply-header">
                            Reply as <strong>Jane Smith (Admin)</strong>
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
                                <button type="submit" className="btn btn-primary" disabled={!replyText.trim()}>
                                    <Send size={16} /> Send Reply
                                </button>
                            </div>
                        </form>
                    </div>
                </div>

                {/* Sidebar Context Panel */}
                <div className="submission-sidebar">
                    <div className="context-card glass-panel">
                        <h3>Delegation</h3>
                        <p className="help-text">Assign this submission to a specific manager to handle.</p>
                        <select className="assign-select">
                            <option value="">Unassigned</option>
                            <option value="m1">Mike Johnson</option>
                            <option value="m2">Sarah Williams</option>
                        </select>
                        <button className="btn btn-ghost btn-full mt-2">Update Assignment</button>
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
