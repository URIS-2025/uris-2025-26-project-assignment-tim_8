import React, { useState, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Lock, User, ArrowRight, Shield, AlertCircle } from 'lucide-react';
import { AnonymousUserService } from '../services/anonymousUserService';
import { useAuth } from '../context/AuthContext';
import TurnstileWidget from '../components/TurnstileWidget';
import './Login.css';

const AnonymousLogin = () => {
    const navigate = useNavigate();
    const { login } = useAuth();
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const [captchaToken, setCaptchaToken] = useState('');
    const captchaRef = useRef(null);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');

        const username = e.target.username.value.trim();
        const password = e.target.password.value;

        if (!username || !password) {
            setError('Please fill in all fields.');
            return;
        }

        if (!captchaToken) {
            setError('Please complete the captcha.');
            return;
        }

        try {
            setIsSubmitting(true);

            // Call the real login endpoint
            const token = await AnonymousUserService.login({
                username,
                password,
                captchaToken
            });

            if (!token) {
                throw new Error('No token received from server');
            }

            // Use AuthContext to store the login state
            await login(token);

            // Navigate to the next page
            navigate('/portal');
        } catch (err) {
            console.error(err);
            setError(err.message || 'Login failed. Please try again.');
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
                            <Shield size={28} color="white" />
                        </div>
                    </div>
                    <h2>Anonymous Sign In</h2>
                    <p>Sign in with your anonymous credentials</p>
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
                            placeholder="Username"
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
                        {isSubmitting ? 'Signing in...' : 'Sign In'} <ArrowRight size={18} />
                    </button>
                </form>

                <div className="auth-footer">
                    <p>
                        Don't have an anonymous account?{' '}
                        <Link to="/anonymous/signup" className="switch-mode-btn" style={{ textDecoration: 'none' }}>
                            Sign up
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

export default AnonymousLogin;
