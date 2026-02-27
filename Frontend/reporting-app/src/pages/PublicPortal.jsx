import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Shield, Send, ArrowRight, CheckCircle, ThumbsUp, MessageSquare, AlertTriangle } from 'lucide-react';
import './PublicPortal.css';

// Mock data
const mockPublicSuggestions = [
    { id: 'S-1', title: 'Start a 4-day work week trial', content: 'Many companies are seeing increased productivity with a 4-day work week. We should trial this for the engineering team.', votes: 142, comments: 12, time: '2 days ago' },
    { id: 'S-2', title: 'More vegetarian options in the cafeteria', content: 'The current menu is very meat-heavy. We need more diverse plant-based options.', votes: 89, comments: 34, time: '1 week ago' },
    { id: 'S-3', title: 'Quarterly Hackathons', content: 'We should host internal hackathons to foster innovation and cross-team collaboration.', votes: 56, comments: 8, time: '2 weeks ago' },
];

const PublicPortal = () => {
    const [activeTab, setActiveTab] = useState('submit'); // 'submit' or 'browse'
    const [submissionType, setSubmissionType] = useState('suggestion');
    const [isSubmitted, setIsSubmitted] = useState(false);

    const [formData, setFormData] = useState({
        title: '',
        content: ''
    });

    const handleSubmit = (e) => {
        e.preventDefault();
        if (!formData.title || !formData.content) return;

        // Simulate submission
        setTimeout(() => {
            setIsSubmitted(true);
        }, 800);
    };

    return (
        <div className="portal-container animate-fade-in">

            <div className="portal-header">
                <div className="portal-header-content glass-panel">
                    <div className="portal-org-meta">
                        <span className="portal-org-name">Tech Corp International</span>
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
                                <p>Your report has been encrypted and sent to the organization administrators.</p>
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
                                    setFormData({ title: '', content: '' });
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

                                    <button type="submit" className="btn btn-primary btn-lg submit-btn bounce-hover">
                                        <Send size={18} /> Submit Securely <ArrowRight size={18} />
                                    </button>
                                </form>
                            </div>
                        )}
                    </div>
                ) : (
                    <div className="browse-section fade-in">
                        <div className="board-filters">
                            <h3>Popular Suggestions</h3>
                            <div className="filter-sort">
                                <select className="portal-select">
                                    <option>Most Voted</option>
                                    <option>Most Recent</option>
                                    <option>Most Commented</option>
                                </select>
                            </div>
                        </div>

                        <div className="suggestions-list">
                            {mockPublicSuggestions.map(suggestion => (
                                <div key={suggestion.id} className="public-suggestion-card glass-panel">
                                    <div className="vote-column">
                                        <button className="vote-btn">
                                            <ThumbsUp size={20} />
                                        </button>
                                        <span className="vote-count">{suggestion.votes}</span>
                                    </div>
                                    <div className="suggestion-content">
                                        <h3 className="suggestion-title">{suggestion.title}</h3>
                                        <p className="suggestion-desc">{suggestion.content}</p>
                                        <div className="suggestion-meta">
                                            <span><MessageSquare size={14} /> {suggestion.comments} comments</span>
                                            <span>&bull;</span>
                                            <span>{suggestion.time}</span>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                        <button className="btn btn-ghost load-more-btn">Load More Ideas</button>
                    </div>
                )}
            </div>
        </div>
    );
};

export default PublicPortal;
