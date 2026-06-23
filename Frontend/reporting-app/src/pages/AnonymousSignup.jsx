import React, { useState, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Lock, User, ArrowRight, UserPlus, ShieldCheck, AlertCircle } from 'lucide-react';
import { AnonymousUserService } from '../services/anonymousUserService';
import { validatePassword, validateUsername } from '../utils/validation';
import { checkPwned } from '../services/pwnedService';
import TurnstileWidget from '../components/TurnstileWidget';
import './Login.css';

const passwordStrength = (password) => {
    if (!password) return { label: '', color: '' };
    if (password.length < 8) return { label: 'Too short', color: '#ef4444' };
    const strong = /^(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{8,}$/.test(password);
    if (strong) return { label: 'Strong', color: '#22c55e' };
    const medium = password.length >= 8 && (/[A-Z]/.test(password) || /[0-9]/.test(password));
    if (medium) return { label: 'Medium', color: '#f59e0b' };
    return { label: 'Weak', color: '#ef4444' };
};

const AnonymousSignup = () => {
    const navigate = useNavigate();
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const [password, setPassword] = useState('');
    const [captchaToken, setCaptchaToken] = useState('');
    const captchaRef = useRef(null);

    const strength = passwordStrength(password);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');

        const username = e.target.username.value.trim();
        const confirmPassword = e.target.confirmPassword.value;

        if (!username || !password) {
            setError('Please fill in all fields.');
            return;
        }

        const uRes = validateUsername(username);
        if (!uRes.valid) {
            setError(uRes.message);
            return;
        }

        const pRes = validatePassword(password);
        if (!pRes.valid) {
            setError(pRes.message);
            return;
        }

        if (password !== confirmPassword) {
            setError('Passwords do not match.');
            return;
        }

        if (!captchaToken) {
            setError('Please complete the captcha.');
            return;
        }

        const pwned = await checkPwned(password);
        if (pwned === 'breached') {
            setError('This password has appeared in a data breach. Please choose another.');
            return;
        }

        try {
            setIsSubmitting(true);
            await AnonymousUserService.create({ username, password, captchaToken });
            navigate('/anonymous/login');
        } catch (err) {
            setError(err.message || 'Failed to create anonymous account.');
            // Turnstile tokens are single-use — get a fresh one for the retry.
            setCaptchaToken('');
            captchaRef.current?.reset();
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

                {error && (
                    <div style={{
                        display: 'flex', alignItems: 'flex-start', gap: '0.6rem',
                        padding: '0.75rem 1rem', borderRadius: 'var(--radius-md)',
                        background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.3)',
                        fontSize: '0.875rem', color: '#fca5a5', marginBottom: '0.5rem'
                    }}>
                        <AlertCircle size={16} style={{ flexShrink: 0, marginTop: '0.1rem' }} />
                        <span>{error}</span>
                    </div>
                )}

                <form className="auth-form" onSubmit={handleSubmit}>
                    <div className="input-group">
                        <User className="input-icon" size={20} />
                        <input
                            type="text"
                            name="username"
                            placeholder="Choose a Username (3–30 chars, letters/digits/_)"
                            className="input-field glass-panel"
                            required
                            minLength={3}
                            maxLength={30}
                            pattern="^[a-zA-Z0-9_]+$"
                            title="Username may only contain letters, digits, and underscores"
                        />
                    </div>

                    <div className="input-group">
                        <Lock className="input-icon" size={20} />
                        <input
                            type="password"
                            name="password"
                            placeholder="Password (min. 8 characters)"
                            className="input-field glass-panel"
                            required
                            minLength={8}
                            maxLength={64}
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                        />
                    </div>

                    {password && (
                        <p style={{ fontSize: '0.8rem', margin: '-0.25rem 0 0.25rem 0.25rem', color: strength.color }}>
                            Password strength: {strength.label}
                        </p>
                    )}

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

                    <TurnstileWidget
                        ref={captchaRef}
                        onVerify={setCaptchaToken}
                        onError={() => setError('Security check failed to load. Disable any ad/script blockers and refresh the page.')}
                    />

                    <button
                        type="submit"
                        className="btn btn-primary btn-full bounce-hover"
                        disabled={isSubmitting || !captchaToken}
                    >
                        {isSubmitting ? 'Creating...' : 'Create Anonymous Account'} {!isSubmitting && <ArrowRight size={18} />}
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
