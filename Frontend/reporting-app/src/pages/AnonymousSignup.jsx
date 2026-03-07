import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Lock, User, ArrowRight, UserPlus, ShieldCheck } from 'lucide-react';
import { AnonymousUserService } from '../services/anonymousUserService';
import './Login.css';

const AnonymousSignup = () => {
    const navigate = useNavigate();
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();

        const username = e.target.username.value.trim();
        const password = e.target.password.value;
        const confirmPassword = e.target.confirmPassword.value;

        if (!username || !password) {
            alert('Please fill in all fields.');
            return;
        }

        if (password !== confirmPassword) {
            alert('Passwords do not match.');
            return;
        }

        try {
            setIsSubmitting(true);
            await AnonymousUserService.create({ username, password });
            alert('Anonymous account created successfully! Please log in.');
            navigate('/anonymous/login');
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to create anonymous account.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-card glass-panel animate-fade-in">
                <div className="auth-header">
                    <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '1rem' }}>
                        <div style={{
                            width: 56, height: 56, borderRadius: '50%',
                            background: 'linear-gradient(135deg, #6366f1, #a855f7)',
                            display: 'flex', alignItems: 'center', justifyContent: 'center'
                        }}>
                            <UserPlus size={28} color="white" />
                        </div>
                    </div>
                    <h2>Anonymous Sign Up</h2>
                    <p>Create an anonymous account to submit feedback securely</p>
                </div>

                <form className="auth-form" onSubmit={handleSubmit}>
                    <div className="input-group">
                        <User className="input-icon" size={20} />
                        <input
                            type="text"
                            name="username"
                            placeholder="Choose a Username"
                            className="input-field glass-panel"
                            required
                        />
                    </div>

                    <div className="input-group">
                        <Lock className="input-icon" size={20} />
                        <input
                            type="password"
                            name="password"
                            placeholder="Password"
                            className="input-field glass-panel"
                            required
                        />
                    </div>

                    <div className="input-group">
                        <ShieldCheck className="input-icon" size={20} />
                        <input
                            type="password"
                            name="confirmPassword"
                            placeholder="Confirm Password"
                            className="input-field glass-panel"
                            required
                        />
                    </div>

                    <div style={{
                        display: 'flex', alignItems: 'center', gap: '0.75rem',
                        padding: '0.85rem 1rem', borderRadius: 'var(--radius-md)',
                        background: 'rgba(99, 102, 241, 0.08)', border: '1px solid rgba(99, 102, 241, 0.15)',
                        fontSize: '0.85rem', color: 'var(--text-secondary)'
                    }}>
                        <ShieldCheck size={18} style={{ color: 'var(--accent-primary)', flexShrink: 0 }} />
                        <span>No email required. Your identity stays completely anonymous.</span>
                    </div>

                    <button
                        type="submit"
                        className="btn btn-primary btn-full bounce-hover"
                        disabled={isSubmitting}
                    >
                        {isSubmitting ? 'Creating...' : 'Create Anonymous Account'} <ArrowRight size={18} />
                    </button>
                </form>

                <div className="auth-footer">
                    <p>
                        Already have an anonymous account?{' '}
                        <Link to="/anonymous/login" className="switch-mode-btn" style={{ textDecoration: 'none' }}>
                            Sign in
                        </Link>
                    </p>
                </div>
            </div>

            <div className="auth-bg-shapes">
                <div className="shape shape-1"></div>
                <div className="shape shape-2"></div>
            </div>
        </div>
    );
};

export default AnonymousSignup;
