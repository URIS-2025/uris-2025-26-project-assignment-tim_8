import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Lock, User, ArrowRight, Shield } from 'lucide-react';
import { AnonymousUserService } from '../services/anonymousUserService';
import { useAuth } from '../context/AuthContext';
import './Login.css';

const AnonymousLogin = () => {
    const navigate = useNavigate();
    const { login } = useAuth();
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();

        const username = e.target.username.value.trim();
        const password = e.target.password.value;

        if (!username || !password) {
            alert('Please fill in all fields.');
            return;
        }

        try {
            setIsSubmitting(true);

            // Call the real login endpoint
            const token = await AnonymousUserService.login({
                username,
                password
            });

            if (!token) {
                throw new Error('No token received from server');
            }

            // Use AuthContext to store the login state
            await login(token);

            // Navigate to the next page
            navigate('/portal');
        } catch (error) {
            console.error(error);
            alert(error.message || 'Login failed. Please try again.');
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

                    <button
                        type="submit"
                        className="btn btn-primary btn-full bounce-hover"
                        disabled={isSubmitting}
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
