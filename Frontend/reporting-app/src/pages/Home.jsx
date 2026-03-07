import React from 'react';
import { Link } from 'react-router-dom';
import { Shield, EyeOff, Lock, ArrowRight, MessageSquareWarning } from 'lucide-react';
import './Home.css';

const FeatureCard = ({ icon: Icon, title, description, delay }) => (
    <div className={`feature-card glass-panel animate-fade-in delay-${delay}`}>
        <div className="feature-icon-wrapper">
            <Icon size={24} className="feature-icon" />
        </div>
        <h3 className="feature-title">{title}</h3>
        <p className="feature-description">{description}</p>
    </div>
);

const Home = () => {
    return (
        <div className="home-container">
            {/* Hero Section */}
            <section className="hero-section">
                <div className="hero-content animate-fade-in">
                    <div className="badge-wrapper">
                        <span className="badge glass-panel">
                            <Lock size={14} /> 100% Anonymous & Secure
                        </span>
                    </div>
                    <h1 className="hero-title">
                        Speak up with <span className="text-gradient">confidence.</span>
                    </h1>
                    <p className="hero-subtitle">
                        A secure platform for employees and students to anonymously report issues, suggest improvements, and ensure their voices are heard without fear of retaliation.
                    </p>
                    <div className="hero-actions-container">
                        <div className="auth-portal-section glass-panel animate-slide-up" style={{ padding: '1.5rem', borderRadius: 'var(--radius-lg)', textAlign: 'left', flex: 1 }}>
                            <h3 style={{ fontSize: '1.25rem', marginBottom: '0.5rem', color: 'white', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                <Shield size={20} className="text-gradient-icon" /> Organization Portal
                            </h3>
                            <p style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', marginBottom: '1.25rem' }}>
                                For admins and managers to review and manage submitted reports.
                            </p>
                            <div style={{ display: 'flex', gap: '0.75rem' }}>
                                <Link to="/login" className="btn btn-primary" style={{ flex: 1, justifyContent: 'center' }}>
                                    Sign In
                                </Link>
                                <Link to="/signup" className="btn btn-ghost glass-panel" style={{ flex: 1, justifyContent: 'center' }}>
                                    Sign Up
                                </Link>
                            </div>
                        </div>

                        <div className="auth-portal-section glass-panel animate-slide-up" style={{ padding: '1.5rem', borderRadius: 'var(--radius-lg)', textAlign: 'left', flex: 1, border: '1px solid rgba(168, 85, 247, 0.3)' }}>
                            <h3 style={{ fontSize: '1.25rem', marginBottom: '0.5rem', color: 'white', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                <EyeOff size={20} style={{ color: '#a855f7' }} /> Anonymous Reporter
                            </h3>
                            <p style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', marginBottom: '1.25rem' }}>
                                Submit suggestions or problems without revealing your identity.
                            </p>
                            <div style={{ display: 'flex', gap: '0.75rem' }}>
                                <Link to="/anonymous/login" className="btn btn-primary" style={{ flex: 1, justifyContent: 'center', background: 'linear-gradient(135deg, #a855f7, #6366f1)' }}>
                                    Sign In
                                </Link>
                                <Link to="/anonymous/signup" className="btn btn-ghost glass-panel" style={{ flex: 1, justifyContent: 'center' }}>
                                    Sign Up
                                </Link>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Floating elements representing reports passing securely */}
                <div className="hero-visual animate-fade-in delay-200">
                    <div className="secure-envelope glass-panel">
                        <div className="envelope-icon">
                            <MessageSquareWarning size={48} color="var(--accent-primary)" />
                        </div>
                        <div className="scanning-line"></div>
                        <p className="envelope-text">Encrypting payload...</p>
                    </div>
                </div>
            </section>

            {/* Features Section */}
            <section className="features-section">
                <div className="features-header animate-fade-in delay-300">
                    <h2>Why use SecureReport?</h2>
                    <p>We prioritize your privacy and the integrity of your organization.</p>
                </div>
                <div className="features-grid">
                    <FeatureCard
                        icon={EyeOff}
                        title="Complete Anonymity"
                        description="Your identity is protected. We don't track IP addresses, device info, or any personally identifiable data."
                        delay="100"
                    />
                    <FeatureCard
                        icon={Shield}
                        title="End-to-End Encryption"
                        description="All reports are encrypted before they leave your device and can only be decrypted by authorized organization admins."
                        delay="200"
                    />
                    <FeatureCard
                        icon={Lock}
                        title="Secure Tracking"
                        description="Track your report's progress using a unique, randomly generated phrase that can't be linked back to you."
                        delay="300"
                    />
                </div>
            </section>
        </div>
    );
};

export default Home;
