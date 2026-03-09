import React, { useState, useEffect, useMemo } from 'react';
import { MessageSquare, Heart, Send, User } from 'lucide-react';
import { SuggestionCommentService } from '../../services/suggestionCommentService';
import { SystemNotificationService } from '../../services/systemNotificationService';

const CommunityFeed = ({ suggestions = [], currentAnonUserId, currentUserEmail = 'Anonymous', organizationId, onRefresh }) => {
    const [sortBy, setSortBy] = useState('newest');

    const sortedSuggestions = useMemo(() => {
        const sorted = [...suggestions];
        if (sortBy === 'newest') {
            return sorted.sort((a, b) => new Date(b.createdAt || 0) - new Date(a.createdAt || 0));
        } else if (sortBy === 'oldest') {
            return sorted.sort((a, b) => new Date(a.createdAt || 0) - new Date(b.createdAt || 0));
        }
        return sorted;
    }, [suggestions, sortBy]);

    return (
        <div className="community-feed">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
                <h3 style={{ margin: 0, fontSize: '1.2rem', color: 'var(--text)' }}>Recent Activity</h3>
                <select
                    className="portal-input"
                    style={{ width: 'auto', marginBottom: 0, padding: '0.4rem 0.8rem', borderRadius: 'var(--radius-md)', background: 'var(--bg-card)' }}
                    value={sortBy}
                    onChange={(e) => setSortBy(e.target.value)}
                >
                    <option value="newest">Most Recent</option>
                    <option value="oldest">Oldest</option>
                </select>
            </div>
            {sortedSuggestions.length === 0 ? (
                <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                    <p>No suggestions yet. Be the first to start the conversation!</p>
                </div>
            ) : (
                sortedSuggestions.map(suggestion => (
                    <Post
                        key={suggestion.id}
                        suggestion={suggestion}
                        currentAnonUserId={currentAnonUserId}
                        currentUserEmail={currentUserEmail}
                        organizationId={organizationId}
                        onRefresh={onRefresh}
                    />
                ))
            )}
        </div>
    );
};

const Post = ({ suggestion, currentAnonUserId, currentUserEmail, organizationId, onRefresh }) => {
    const [comments, setComments] = useState([]);
    const [newComment, setNewComment] = useState("");
    const [showComments, setShowComments] = useState(false);
    const [submittingComment, setSubmittingComment] = useState(false);
    const [replyingTo, setReplyingTo] = useState(null); // stores { id, authorName } of the comment being replied to

    // Purely visual implementation per user request, persisting via localStorage
    // Track if THIS specific user liked the post
    const postLikeKey = `user_${currentUserEmail}_post_liked_${suggestion.id}`;
    // Track total count globally on this machine
    const postLikeCountKey = `global_post_like_count_${suggestion.id}`;

    const [isPostLiked, setIsPostLiked] = useState(() => localStorage.getItem(postLikeKey) === 'true');
    const [postLikeCount, setPostLikeCount] = useState(() => {
        const savedCount = localStorage.getItem(postLikeCountKey);
        return savedCount ? parseInt(savedCount, 10) : 0;
    });

    const togglePostLike = () => {
        if (isPostLiked) {
            setPostLikeCount(prev => {
                const newVal = prev - 1;
                localStorage.setItem(postLikeCountKey, newVal.toString());
                return newVal;
            });
            setIsPostLiked(false);
            localStorage.setItem(postLikeKey, 'false');
        } else {
            setPostLikeCount(prev => {
                const newVal = prev + 1;
                localStorage.setItem(postLikeCountKey, newVal.toString());
                return newVal;
            });
            setIsPostLiked(true);
            localStorage.setItem(postLikeKey, 'true');
        }
    };

    // Eagerly load comments so we have the accurate count before expanding
    useEffect(() => {
        fetchComments();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [suggestion.id]);

    const fetchComments = async () => {
        try {
            const data = await SuggestionCommentService.getBySuggestionId(suggestion.id);
            // Sort by oldest first so they appear top-to-bottom
            const sortedComments = data.sort((a, b) => new Date(a.createdAt || 0) - new Date(b.createdAt || 0));
            setComments(sortedComments);
        } catch (err) {
            console.error('Error fetching comments:', err);
        }
    };

    const handleAddComment = async (e) => {
        if (e) e.preventDefault();
        if (!newComment.trim() || submittingComment) return;

        const commentText = newComment;
        setNewComment(""); // Clear immediately for UX
        setSubmittingComment(true);

        try {
            const createdComment = await SuggestionCommentService.create({
                suggestionId: suggestion.id,
                commentText: commentText,
                isAnonymous: true,
                suggestionCommentAuthorId: currentAnonUserId || '00000000-0000-0000-0000-000000000000',
                suggestionCommentId: replyingTo ? replyingTo.id : null,
                createdBy: currentUserEmail
            });

            // Create SystemNotification
            try {
                await SystemNotificationService.create({
                    text: commentText,
                    suggestionCommentId: createdComment?.id || null,
                    organizationId: organizationId || suggestion?.organizationId || null,
                    anonymousUserId: currentAnonUserId || null
                });
            } catch (notifErr) {
                console.error('Failed to create system notification:', notifErr);
            }

            // On success:
            setReplyingTo(null);
            setShowComments(true); // Force open if closed 

            // Re-fetch all comments to get the real ones
            await fetchComments();

            if (onRefresh) onRefresh();
        } catch (err) {
            console.error('Error adding comment:', err);
            // Optionally, we could put the text back in the box if it failed:
            // setNewComment(commentText);
        } finally {
            setSubmittingComment(false);
        }
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleAddComment();
        }
    };

    // Group comments into top-level and replies
    const topLevelComments = comments.filter(c => !c.suggestionCommentId);
    const getReplies = (parentId) => comments.filter(c => c.suggestionCommentId === parentId);

    const handleReplyClick = (commentId, authorName) => {
        setReplyingTo({ id: commentId, authorName });
        // Optional: Focus the input field here if we had a ref
    };

    return (
        <div className="post-card glass-panel" style={{ marginBottom: '1.5rem', padding: '1.5rem', borderRadius: 'var(--radius-lg)' }}>
            <div className="post-header" style={{ display: 'flex', alignItems: 'center', marginBottom: '1rem' }}>
                <div className="post-avatar" style={{
                    width: '40px', height: '40px', borderRadius: '50%',
                    background: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'white', marginRight: '1rem'
                }}>
                    <User size={20} />
                </div>
                <div className="post-meta">
                    <h4 style={{ margin: 0, fontSize: '1.1rem' }}>{suggestion.title}</h4>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                        {new Date(suggestion.createdAt || Date.now()).toLocaleDateString()} at {new Date(suggestion.createdAt || Date.now()).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} &bull; Anonymous
                    </span>
                </div>
            </div>

            <div className="post-body" style={{ marginBottom: '1.5rem' }}>
                <p style={{ lineHeight: '1.6' }}>{suggestion.description}</p>
            </div>

            <div className="post-actions" style={{ display: 'flex', gap: '1rem', borderTop: '1px solid rgba(255,255,255,0.1)', paddingTop: '1rem' }}>
                <button
                    className="btn btn-ghost"
                    onClick={togglePostLike}
                    style={{
                        flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem',
                        color: isPostLiked ? '#a855f7' : 'var(--text-muted)'
                    }}
                >
                    <Heart size={18} fill={isPostLiked ? '#a855f7' : 'none'} color={isPostLiked ? '#a855f7' : 'currentColor'} />
                    {postLikeCount > 0 && <span style={{ fontWeight: 600 }}>{postLikeCount}</span>}
                    <span>{(postLikeCount === 1) ? 'Like' : 'Likes'}</span>
                </button>
                <button
                    className="btn btn-ghost"
                    style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', color: showComments ? 'var(--primary)' : 'var(--text-muted)' }}
                    onClick={() => setShowComments(!showComments)}
                >
                    <MessageSquare size={18} /> {comments.length} Comments
                </button>
            </div>

            {showComments && (
                <div className="post-comments-section" style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid rgba(255,255,255,0.1)' }}>
                    <div className="comments-list" style={{ marginBottom: '1rem' }}>
                        {topLevelComments.length === 0 ? (
                            <p style={{ fontSize: '0.9rem', color: 'var(--text-muted)', textAlign: 'center' }}>No comments yet.</p>
                        ) : (
                            topLevelComments.map(comment => (
                                <div key={comment.id}>
                                    <CommentItem
                                        comment={comment}
                                        currentUserEmail={currentUserEmail}
                                        onReply={() => handleReplyClick(comment.id, comment.createdBy || 'Anonymous')}
                                    />
                                    {/* Render Replies */}
                                    {getReplies(comment.id).length > 0 && (
                                        <div className="replies" style={{ marginLeft: '2.5rem', paddingLeft: '1rem', borderLeft: '2px solid rgba(255,255,255,0.05)' }}>
                                            {getReplies(comment.id).map(reply => (
                                                <CommentItem
                                                    key={reply.id}
                                                    comment={reply}
                                                    isReply={true}
                                                    currentUserEmail={currentUserEmail}
                                                    onReply={() => handleReplyClick(comment.id, reply.createdBy || 'Anonymous')} // Still reply to the top thread
                                                />
                                            ))}
                                        </div>
                                    )}
                                </div>
                            ))
                        )}
                    </div>

                    {replyingTo && (
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'rgba(255,255,255,0.05)', padding: '0.5rem 1rem', borderRadius: 'var(--radius-md) var(--radius-md) 0 0', fontSize: '0.85rem' }}>
                            <span>Replying to <strong>{replyingTo.authorName}</strong></span>
                            <button onClick={() => setReplyingTo(null)} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>Cancel</button>
                        </div>
                    )}
                    <form className="comment-form" onSubmit={handleAddComment} style={{ display: 'flex', gap: '0.5rem' }}>
                        <input
                            type="text"
                            className="portal-input"
                            placeholder={replyingTo ? "Write a reply..." : "Write a comment..."}
                            value={newComment}
                            onChange={(e) => setNewComment(e.target.value)}
                            onKeyDown={handleKeyDown}
                            style={{ flex: 1, marginBottom: 0, padding: '0.75rem', borderRadius: replyingTo ? '0 0 var(--radius-lg) var(--radius-lg)' : 'var(--radius-full)' }}
                        />
                        <button
                            type="submit"
                            className="btn btn-primary"
                            disabled={submittingComment || !newComment.trim()}
                            style={{ borderRadius: '50%', width: '45px', height: '45px', padding: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}
                        >
                            <Send size={18} />
                        </button>
                    </form>
                </div>
            )}
        </div>
    );
};

const CommentItem = ({ comment, onReply, isReply = false, currentUserEmail }) => {
    // Purely visual implementation per user request, persisting via localStorage
    // Track if THIS specific user liked the comment
    const commentLikeKey = `user_${currentUserEmail}_comment_liked_${comment.id}`;
    // Track total count globally on this machine
    const commentLikeCountKey = `global_comment_like_count_${comment.id}`;

    const [isLiked, setIsLiked] = useState(() => localStorage.getItem(commentLikeKey) === 'true');
    const [likeCount, setLikeCount] = useState(() => {
        const savedCount = localStorage.getItem(commentLikeCountKey);
        return savedCount ? parseInt(savedCount, 10) : 0;
    });

    const toggleLike = () => {
        if (isLiked) {
            setLikeCount(prev => {
                const newVal = prev - 1;
                localStorage.setItem(commentLikeCountKey, newVal.toString());
                return newVal;
            });
            setIsLiked(false);
            localStorage.setItem(commentLikeKey, 'false');
        } else {
            setLikeCount(prev => {
                const newVal = prev + 1;
                localStorage.setItem(commentLikeCountKey, newVal.toString());
                return newVal;
            });
            setIsLiked(true);
            localStorage.setItem(commentLikeKey, 'true');
        }
    };

    return (
        <div className="comment-item" style={{ display: 'flex', marginBottom: isReply ? '0.75rem' : '1rem' }}>
            <div className="comment-avatar" style={{
                width: isReply ? '24px' : '32px', height: isReply ? '24px' : '32px', borderRadius: '50%',
                background: 'rgba(255,255,255,0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'white', marginRight: '0.75rem', flexShrink: 0
            }}>
                <User size={isReply ? 14 : 16} />
            </div>
            <div className="comment-content" style={{ flex: 1 }}>
                <div style={{ background: 'rgba(0,0,0,0.2)', padding: '0.75rem 1rem', borderRadius: 'var(--radius-lg)' }}>
                    <div style={{ fontWeight: 600, fontSize: '0.85rem', marginBottom: '0.25rem' }}>
                        {comment.createdBy || 'Anonymous'} <span style={{ fontWeight: 400, color: 'var(--text-muted)', fontSize: '0.75rem', marginLeft: '0.5rem' }}>
                            {new Date(comment.createdAt || Date.now()).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </span>
                    </div>
                    <p style={{ fontSize: '0.9rem', margin: 0 }}>{comment.commentText || comment.text}</p>
                </div>
                <div className="comment-actions" style={{ display: 'flex', alignItems: 'center', marginTop: '0.25rem', paddingLeft: '0.5rem', gap: '1rem' }}>
                    <button
                        className="btn btn-ghost"
                        onClick={toggleLike}
                        style={{
                            padding: '0.25rem 0.5rem', fontSize: '0.8rem', height: 'auto',
                            color: isLiked ? '#a855f7' : 'var(--text-muted)',
                            display: 'flex', alignItems: 'center', gap: '0.25rem'
                        }}
                    >
                        <Heart size={14} fill={isLiked ? '#a855f7' : 'none'} color={isLiked ? '#a855f7' : 'currentColor'} />
                        {likeCount > 0 && <span style={{ fontWeight: 600 }}>{likeCount}</span>}
                        <span style={{ marginLeft: '2px' }}>{(likeCount === 1) ? 'Like' : 'Likes'}</span>
                    </button>
                    <button onClick={onReply} className="btn btn-ghost" style={{ padding: '0.2rem 0.5rem', fontSize: '0.8rem', height: 'auto', color: 'var(--text-muted)' }}>
                        Reply
                    </button>
                </div>
            </div>
        </div>
    );
};

export default CommunityFeed;
