import React, { useState } from 'react';
import { ArrowRight, Shield, Search, Lock, Clock, Send, CheckCircle, AlertOctagon, MessageSquareWarning } from 'lucide-react';
import StatusBadge from '../components/StatusBadge';
import './TrackReport.css';

const TrackReport = () => {
    const [trackingPhrase, setTrackingPhrase] = useState('');
    const [isSearching, setIsSearching] = useState(false);
    const [reportFound, setReportFound] = useState(null);
    const [replyText, setReplyText] = useState('');

    // Mock search handler
    const handleSearch = (e) => {
        e.preventDefault();
        if (!trackingPhrase.trim()) return;

        setIsSearching(true);

        // Simulate API delay
        setTimeout(() => {
            // Mock result (in real app, this would fetch from backend using the Session token/phrase)
            setReportFound({
                id: 'SUB-101',
                boxName: 'Facilities & Maintenance',
                orgName: 'Tech Corp',
                title: 'Coffee machine in breakroom is broken',
                content: 'The coffee machine on the 2nd floor has been leaking water since Tuesday. It needs maintenance immediately as it is creating a slip hazard near the power outlets.',
                date: 'Oct 24, 2023, 10:30 AM',
                status: 'In Progress',
                type: 'problem',
                thread: [
                    { id: 1, sender: 'author', role: 'You', text: 'Please send someone soon, the puddle is getting bigger.', time: 'Oct 24, 2023, 11:15 AM' },
                    { id: 2, sender: 'admin', role: 'Support Agent', text: 'Thank you for reporting this. I have contacted building maintenance, they should arrive within the hour.', time: 'Oct 24, 2023, 11:45 AM' }
                ]
            });
            setIsSearching(false);
        }, 1000);
    };

    const handleReplySubmit = (e) => {
        e.preventDefault();
        if (!replyText.trim()) return;

        // Simulate updating the thread
        const newReply = {
            id: Date.now(),
            sender: 'author',
            role: 'You',
            text: replyText,
            time: 'Just now'
        };

        setReportFound({
            ...reportFound,
            thread: [...reportFound.thread, newReply]
        });
        setReplyText('');
    };

    return (
        <div className="track-container animate-fade-in">
            {!reportFound ? (
                <div className="track-search-card glass-panel">
                    <div className="search-header">
                        <div className="secure-icon-wrapper">
                            <Lock size={32} className="secure-icon" />
                        </div>
                        <h1>Track Your Report</h1>
                        <p>Enter your secure <strong>tracking phrase</strong> to view updates, check the status, or communicate privately with administrators.</p>
                    </div>

                    <form onSubmit={handleSearch} className="track-search-form">
                        <div className="track-input-group">
                            <Search className="input-icon" size={20} />
                            <input
                                type="text"
                                placeholder="e.g., purple-elephant-jumping-high"
                                className="track-input"
                                value={trackingPhrase}
                                onChange={(e) => setTrackingPhrase(e.target.value)}
                                required
                            />
                        </div>
                        <button
                            type="submit"
                            className="btn btn-primary btn-full btn-lg bounce-hover"
                            disabled={isSearching}
                        >
                            {isSearching ? 'Decrypting Record...' : 'Access Report'} <ArrowRight size={18} />
                        </button>
                    </form>

                    <div className="privacy-notice">
                        <Shield size={16} />
                        <span>Your tracking phrase is the only way to access this report. We cannot recover it if lost to protect your anonymity.</span>
                    </div>
                </div>
            ) : (
                <div className="tracked-report-view">
                    {/* Header Bar */}
                    <div className="tracked-header glass-panel">
                        <div className="tracked-meta">
                            <div className="organization-info">
                                <span className="org-label">{reportFound.orgName}</span>
                                <span className="box-name">{reportFound.boxName}</span>
                            </div>
                            <div className="status-container">
                                <span className="status-label">Current Status:</span>
                                <StatusBadge type="status" status={reportFound.status} />
                            </div>
                        </div>

                        <button className="btn btn-ghost" onClick={() => setReportFound(null)}>
                            Exit Secure Session
                        </button>
                    </div>

                    {/* Report Details and Thread */}
                    <div className="tracked-content-wrapper">

                        <div className="tracked-main-column">
                            {/* Original Post */}
                            <div className="tracked-original-post glass-panel">
                                <div className="post-type-badge">
                                    {reportFound.type === 'problem' ? (
                                        <><AlertOctagon size={16} className="text-warning" /> Problem Report</>
                                    ) : (
                                        <><MessageSquareWarning size={16} className="text-success" /> Suggestion</>
                                    )}
                                </div>
                                <h2>{reportFound.title}</h2>
                                <div className="post-meta">
                                    <span className="post-id">ID: {reportFound.id}</span>
                                    <span className="post-date"><Clock size={14} /> {reportFound.date}</span>
                                </div>
                                <div className="post-body">
                                    <p>{reportFound.content}</p>
                                </div>
                            </div>

                            <div className="thread-divider">
                                <span>Secure Communications</span>
                            </div>

                            {/* Thread */}
                            <div className="thread-container">
                                {reportFound.thread.map((msg) => (
                                    <div key={msg.id} className={`message-bubble ${msg.sender} glass-panel`}>
                                        <div className="message-header">
                                            <div className="message-author">
                                                <span className="author-name">{msg.role}</span>
                                                {msg.sender === 'admin' && (
                                                    <span className="author-badge"><CheckCircle size={10} style={{ display: 'inline', marginRight: '2px' }} /> Verified Auth</span>
                                                )}
                                            </div>
                                            <span className="message-time">{msg.time}</span>
                                        </div>
                                        <div className="message-body">
                                            <p>{msg.text}</p>
                                        </div>
                                    </div>
                                ))}
                            </div>

                            {/* Private Reply Box */}
                            <div className="reply-box glass-panel">
                                <div className="reply-header">
                                    <strong>Private Secure Reply</strong> (Sent as Anonymous)
                                </div>
                                <form onSubmit={handleReplySubmit}>
                                    <textarea
                                        className="reply-textarea"
                                        placeholder="Provide additional details or reply to the administrator..."
                                        value={replyText}
                                        onChange={(e) => setReplyText(e.target.value)}
                                    />
                                    <div className="reply-actions">
                                        <button type="submit" className="btn btn-primary" disabled={!replyText.trim()}>
                                            <Send size={16} /> Send Secure Message
                                        </button>
                                    </div>
                                </form>
                            </div>

                        </div>

                        <div className="tracked-sidebar">
                            <div className="info-card glass-panel">
                                <h3><Shield size={18} /> Identity Protection</h3>
                                <p>You are currently accessing this thread in a secure session using your tracking phrase.</p>
                                <div className="divider"></div>
                                <ul>
                                    <li>Your identity remains completely hidden.</li>
                                    <li>No device info or IP is shared with the organization.</li>
                                    <li>Close this tab when finished if using a shared computer.</li>
                                </ul>
                            </div>
                        </div>

                    </div>
                </div>
            )}
        </div>
    );
};

export default TrackReport;
