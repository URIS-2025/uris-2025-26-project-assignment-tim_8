import React from 'react';

const StatusBadge = ({ type, status }) => {
    // Define styles for different badge types
    const getBadgeStyle = () => {
        switch (type) {
            case 'priority':
                switch (status?.toLowerCase()) {
                    case 'high':
                        return { color: 'var(--danger)', bg: 'rgba(239, 68, 68, 0.15)', border: 'rgba(239, 68, 68, 0.3)' };
                    case 'medium':
                        return { color: 'var(--warning)', bg: 'rgba(245, 158, 11, 0.15)', border: 'rgba(245, 158, 11, 0.3)' };
                    case 'low':
                        return { color: 'var(--success)', bg: 'rgba(34, 197, 94, 0.15)', border: 'rgba(34, 197, 94, 0.3)' };
                    default:
                        return { color: 'var(--text-secondary)', bg: 'rgba(255, 255, 255, 0.05)', border: 'var(--border-subtle)' };
                }
            case 'status':
                switch (status?.toLowerCase()) {
                    case 'new':
                    case 'open':
                        return { color: 'var(--accent-primary)', bg: 'rgba(99, 102, 241, 0.15)', border: 'rgba(99, 102, 241, 0.3)' };
                    case 'in progress':
                    case 'reviewing':
                        return { color: 'var(--warning)', bg: 'rgba(245, 158, 11, 0.15)', border: 'rgba(245, 158, 11, 0.3)' };
                    case 'resolved':
                    case 'closed':
                        return { color: 'var(--success)', bg: 'rgba(34, 197, 94, 0.15)', border: 'rgba(34, 197, 94, 0.3)' };
                    default:
                        return { color: 'var(--text-secondary)', bg: 'rgba(255, 255, 255, 0.05)', border: 'var(--border-subtle)' };
                }
            case 'role':
                switch (status?.toLowerCase()) {
                    case 'admin':
                        return { color: '#ef4444', bg: 'rgba(239, 68, 68, 0.15)', border: 'rgba(239, 68, 68, 0.3)' };
                    case 'manager':
                        return { color: '#a855f7', bg: 'rgba(168, 85, 247, 0.15)', border: 'rgba(168, 85, 247, 0.3)' };
                    default:
                        return { color: 'var(--text-secondary)', bg: 'rgba(255, 255, 255, 0.05)', border: 'var(--border-subtle)' };
                }
            default:
                return { color: 'var(--text-secondary)', bg: 'rgba(255, 255, 255, 0.05)', border: 'var(--border-subtle)' };
        }
    };

    const style = getBadgeStyle();

    return (
        <span
            style={{
                display: 'inline-flex',
                alignItems: 'center',
                padding: '0.25rem 0.75rem',
                borderRadius: '999px',
                fontSize: '0.75rem',
                fontWeight: '600',
                lineHeight: '1.25',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                color: style.color,
                backgroundColor: style.bg,
                border: `1px solid ${style.border}`,
            }}
        >
            {status}
        </span>
    );
};

export default StatusBadge;
