import React, { useState, useEffect } from 'react';
import { Loader2 } from 'lucide-react';
import { SuggestionService } from '../services/suggestionService';
import CommunityFeed from '../components/Community/CommunityFeed';
import { useAuth } from '../context/AuthContext';

const CommunityDashboard = () => {
    const [suggestions, setSuggestions] = useState([]);
    const [loading, setLoading] = useState(true);
    const { user } = useAuth();

    const fetchSuggestions = async () => {
        try {
            setLoading(true);
            const data = await SuggestionService.getAll();
            setSuggestions(data);
        } catch (err) {
            console.error('Error fetching suggestions:', err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchSuggestions();
    }, []);

    const adminAnonUserId = user?.id || '00000000-0000-0000-0000-000000000000';

    return (
        <div className="page-container animate-fade-in">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Community Board</h1>
                    <p className="page-subtitle">View and interact with all anonymous suggestions and feedback</p>
                </div>
            </div>

            <div className="page-content" style={{ maxWidth: '800px', margin: '0 auto' }}>
                {loading ? (
                    <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                        <Loader2 size={32} style={{ animation: 'spin 1s linear infinite', marginBottom: '1rem' }} />
                        <p>Loading community feed...</p>
                    </div>
                ) : (
                    <CommunityFeed
                        suggestions={suggestions}
                        currentAnonUserId={adminAnonUserId}
                        currentUserEmail={user?.email || 'Admin/Manager'}
                        onRefresh={fetchSuggestions}
                    />
                )}
            </div>
        </div>
    );
};

export default CommunityDashboard;
