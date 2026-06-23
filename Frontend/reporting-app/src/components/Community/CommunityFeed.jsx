import React, { useState, useEffect, useMemo } from 'react';
import { MessageSquare, Heart, Send, User } from 'lucide-react';
import { SuggestionCommentService } from '../../services/suggestionCommentService';
import { SystemNotificationService } from '../../services/systemNotificationService';
import './CommunityFeed.css';

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
            <div className="cf-feed-header">
                <h3 className="cf-feed-title">Recent Activity</h3>
                <select
                    className="form-control cf-sort-select"
                    value={sortBy}
                    onChange={(e) => setSortBy(e.target.value)}
                >
                    <option value="newest">Most Recent</option>
                    <option value="oldest">Oldest</option>
                </select>
            </div>
            {sortedSuggestions.length === 0 ? (
                <div className="cf-empty">
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
                text: commentText,
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
        <div className="post-card glass-panel cf-post">
            <div className="post-header cf-post-header">
                <div className="post-avatar cf-avatar">
                    <User size={20} />
                </div>
                <div className="post-meta">
                    <h4 className="cf-post-title">{suggestion.title}</h4>
                    <span className="cf-post-time">
                        {new Date(suggestion.createdAt || Date.now()).toLocaleDateString()} at {new Date(suggestion.createdAt || Date.now()).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} &bull; Anonymous
                    </span>
                </div>
            </div>

            <div className="post-body cf-post-body">
                <p>{suggestion.description}</p>
            </div>

            <div className="post-actions cf-post-actions">
                <button
                    className="btn btn-ghost cf-action-btn"
                    onClick={togglePostLike}
                    aria-label={isPostLiked ? 'Unlike this suggestion' : 'Like this suggestion'}
                    aria-pressed={isPostLiked}
                    style={{ color: isPostLiked ? 'var(--accent-secondary)' : 'var(--text-muted)' }}
                >
                    <Heart size={18} fill={isPostLiked ? 'var(--accent-secondary)' : 'none'} color={isPostLiked ? 'var(--accent-secondary)' : 'currentColor'} />
                    {postLikeCount > 0 && <span className="cf-action-count">{postLikeCount}</span>}
                    <span>{(postLikeCount === 1) ? 'Like' : 'Likes'}</span>
                </button>
                <button
                    className="btn btn-ghost cf-action-btn"
                    style={{ color: showComments ? 'var(--accent-primary)' : 'var(--text-muted)' }}
                    onClick={() => setShowComments(!showComments)}
                    aria-expanded={showComments}
                >
                    <MessageSquare size={18} /> {comments.length} Comments
                </button>
            </div>

            {showComments && (
                <div className="post-comments-section cf-comments-section">
                    <div className="comments-list cf-comments-list">
                        {topLevelComments.length === 0 ? (
                            <p className="cf-comments-empty">No comments yet.</p>
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
                                        <div className="replies cf-replies">
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
                        <div className="cf-reply-banner">
                            <span>Replying to <strong>{replyingTo.authorName}</strong></span>
                            <button onClick={() => setReplyingTo(null)} className="cf-reply-cancel">Cancel</button>
                        </div>
                    )}
                    <form className="comment-form cf-comment-form" onSubmit={handleAddComment}>
                        <input
                            type="text"
                            className="form-control cf-comment-input"
                            placeholder={replyingTo ? "Write a reply..." : "Write a comment..."}
                            value={newComment}
                            onChange={(e) => setNewComment(e.target.value)}
                            onKeyDown={handleKeyDown}
                            style={{ borderRadius: replyingTo ? '0 0 var(--radius-lg) var(--radius-lg)' : 'var(--radius-full)' }}
                        />
                        <button
                            type="submit"
                            className="btn btn-primary cf-comment-send"
                            disabled={submittingComment || !newComment.trim()}
                            aria-label="Send comment"
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
        <div className={`comment-item cf-comment${isReply ? ' cf-comment-reply' : ''}`}>
            <div className="comment-avatar cf-comment-avatar">
                <User size={isReply ? 14 : 16} />
            </div>
            <div className="comment-content cf-comment-content">
                <div className="cf-comment-bubble">
                    <div className="cf-comment-author">
                        {comment.createdBy || 'Anonymous'} <span className="cf-comment-time">
                            {new Date(comment.createdAt || Date.now()).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </span>
                    </div>
                    <p className="cf-comment-text">{comment.commentText || comment.text}</p>
                </div>
                <div className="comment-actions cf-comment-actions">
                    <button
                        className="btn btn-ghost cf-comment-like"
                        onClick={toggleLike}
                        aria-label={isLiked ? 'Unlike this comment' : 'Like this comment'}
                        aria-pressed={isLiked}
                        style={{ color: isLiked ? 'var(--accent-secondary)' : 'var(--text-muted)' }}
                    >
                        <Heart size={14} fill={isLiked ? 'var(--accent-secondary)' : 'none'} color={isLiked ? 'var(--accent-secondary)' : 'currentColor'} />
                        {likeCount > 0 && <span className="cf-action-count">{likeCount}</span>}
                        <span className="cf-comment-like-label">{(likeCount === 1) ? 'Like' : 'Likes'}</span>
                    </button>
                    <button onClick={onReply} className="btn btn-ghost cf-comment-reply-btn">
                        Reply
                    </button>
                </div>
            </div>
        </div>
    );
};

export default CommunityFeed;
