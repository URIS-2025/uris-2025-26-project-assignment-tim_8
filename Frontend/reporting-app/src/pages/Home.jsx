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
                    <div className="hero-actions">
                        <Link to="/login?mode=signup" className="btn btn-primary btn-lg">
                            Report an Issue <ArrowRight size={18} />
                        </Link>
                        <Link to="/organizations" className="btn btn-ghost btn-lg glass-panel">
                            View Organizations
                        </Link>
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
