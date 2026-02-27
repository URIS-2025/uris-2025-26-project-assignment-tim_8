import React, { useState, useEffect } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Mail, Lock, User, ArrowRight, Github } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import './Login.css';

const InputField = ({ icon: Icon, type, placeholder, name }) => (
    <div className="input-group">
        <Icon className="input-icon" size={20} />
        <input
            type={type}
            name={name}
            placeholder={placeholder}
            className="input-field glass-panel"
            required
        />
    </div>
);

const Login = () => {
    const location = useLocation();
    const navigate = useNavigate();
    const { login } = useAuth();
    const queryParams = new URLSearchParams(location.search);
    const initialMode = queryParams.get('mode') === 'signup' ? 'signup' : 'login';

    const [mode, setMode] = useState(initialMode);

    // Sync mode with URL if user navigates back/forward
    useEffect(() => {
        setMode(initialMode);
    }, [initialMode]);

    const toggleMode = () => {
        setMode(mode === 'login' ? 'signup' : 'login');
    };

    const handleSubmit = (e) => {
        e.preventDefault();

        // Mock role selection based on email typed in
        const email = e.target.email.value.toLowerCase();
        let role = 'admin'; // default mock

        if (email.includes('manager')) {
            role = 'manager';
        } else if (email.includes('billing')) {
            role = 'billing';
        }

        login(role);
        navigate('/admin/dashboard');
    };

    return (
        <div className="auth-container">
            <div className="auth-card glass-panel animate-fade-in">
                <div className="auth-header">
                    <h2>{mode === 'login' ? 'Welcome Back' : 'Create an Account'}</h2>
                    <p>{mode === 'login'
                        ? 'Sign in to access your dashboard'
                        : 'Join SecureReport to start submitting securely'}
                    </p>
                </div>

                <form className="auth-form" onSubmit={handleSubmit}>
                    {mode === 'signup' && (
                        <InputField
                            icon={User}
                            type="text"
                            name="name"
                            placeholder="Full Name (Optional for reporters)"
                        />
                    )}
                    <InputField
                        icon={Mail}
                        type="email"
                        name="email"
                        placeholder="Email Address"
                    />
                    <InputField
                        icon={Lock}
                        type="password"
                        name="password"
                        placeholder="Password"
                    />

                    {mode === 'login' && (
                        <div className="auth-options">
                            <label className="checkbox-container">
                                <input type="checkbox" />
                                <span className="checkmark"></span>
                                Remember me
                            </label>
                            <Link to="/forgot-password" className="forgot-password">Forgot password?</Link>
                        </div>
                    )}

                    {mode === 'login' && (
                        <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '1rem', textAlign: 'center' }}>
                            <p>Mock Logic: Use <strong>admin@</strong>, <strong>manager@</strong>, or <strong>billing@</strong></p>
                            <p>to login as different roles.</p>
                        </div>
                    )}

                    <button type="submit" className="btn btn-primary btn-full bounce-hover">
                        {mode === 'login' ? 'Sign In' : 'Create Account'} <ArrowRight size={18} />
                    </button>
                </form>

                <div className="auth-footer">
                    <p>
                        {mode === 'login' ? "Don't have an account? " : "Already have an account? "}
                        <button className="switch-mode-btn" onClick={toggleMode}>
                            {mode === 'login' ? 'Sign up' : 'Sign in'}
                        </button>
                    </p>
                </div>
            </div>

            {/* Background visual effects specific to auth page */}
            <div className="auth-bg-shapes">
                <div className="shape shape-1"></div>
                <div className="shape shape-2"></div>
            </div>
        </div>
    );
};

export default Login;
