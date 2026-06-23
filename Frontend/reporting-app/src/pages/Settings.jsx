import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { User, Lock, Bell, Shield } from 'lucide-react';
import './Settings.css';

const Settings = () => {
    const { user } = useAuth();
    const [activeTab, setActiveTab] = useState('profile');

    // If user is null (on initial load before redirect), don't crash
    if (!user) return null;

    return (
        <div className="settings-container animate-fade-in">
            <div className="page-header">
                <div>
                    <h1 className="page-title">Settings</h1>
                    <p className="page-description">Manage your account settings and preferences.</p>
                </div>
            </div>

            <div className="settings-layout">
                <div className="settings-sidebar glass-panel" role="tablist" aria-label="Settings sections">
                    <button
                        role="tab"
                        aria-selected={activeTab === 'profile'}
                        className={`settings-tab ${activeTab === 'profile' ? 'active' : ''}`}
                        onClick={() => setActiveTab('profile')}
                    >
                        <User size={18} /> My Profile
                    </button>
                    <button
                        role="tab"
                        aria-selected={activeTab === 'security'}
                        className={`settings-tab ${activeTab === 'security' ? 'active' : ''}`}
                        onClick={() => setActiveTab('security')}
                    >
                        <Lock size={18} /> Security
                    </button>
                    <button
                        role="tab"
                        aria-selected={activeTab === 'notifications'}
                        className={`settings-tab ${activeTab === 'notifications' ? 'active' : ''}`}
                        onClick={() => setActiveTab('notifications')}
                    >
                        <Bell size={18} /> Notifications
                    </button>
                    {user.role === 'admin' && (
                        <button
                            role="tab"
                            aria-selected={activeTab === 'system'}
                            className={`settings-tab ${activeTab === 'system' ? 'active' : ''}`}
                            onClick={() => setActiveTab('system')}
                        >
                            <Shield size={18} /> System Settings
                        </button>
                    )}
                </div>

                <div className="settings-content glass-panel">
                    {activeTab === 'profile' && (
                        <div className="settings-section fade-in">
                            <h2>Profile Information</h2>
                            <div className="form-group settings-field">
                                <label>Full Name</label>
                                <input type="text" className="form-control" defaultValue={user.name} />
                            </div>
                            <div className="form-group settings-field">
                                <label>Email Address</label>
                                <input type="email" className="form-control" defaultValue={user.email} />
                            </div>
                            <div className="form-group settings-field">
                                <label>Role</label>
                                <input type="text" className="form-control" value={user.role.toUpperCase()} disabled />
                            </div>
                            <button className="btn btn-primary mt-4">Save Changes</button>
                        </div>
                    )}

                    {activeTab === 'security' && (
                        <div className="settings-section fade-in">
                            <h2>Change Password</h2>
                            <div className="form-group settings-field-narrow">
                                <label>Current Password</label>
                                <input type="password" className="form-control" />
                            </div>
                            <div className="form-group settings-field-narrow">
                                <label>New Password</label>
                                <input type="password" className="form-control" />
                            </div>
                            <div className="form-group settings-field-narrow">
                                <label>Confirm New Password</label>
                                <input type="password" className="form-control" />
                            </div>
                            <button className="btn btn-primary mt-4">Update Password</button>

                            <hr className="settings-divider" />

                            <h2>Two-Factor Authentication</h2>
                            <p className="settings-lead">Add an extra layer of security to your account.</p>
                            <button className="btn btn-ghost">Enable 2FA</button>
                        </div>
                    )}

                    {activeTab === 'notifications' && (
                        <div className="settings-section fade-in">
                            <h2>Notification Preferences</h2>
                            <div className="notification-options">
                                <label className="checkbox-container">
                                    <input type="checkbox" defaultChecked />
                                    <span className="checkmark"></span>
                                    <div>
                                        <strong>Email Alerts for New Submissions</strong>
                                        <p className="checkbox-hint">Receive an email when a new submission is added to your boxes.</p>
                                    </div>
                                </label>
                                <label className="checkbox-container">
                                    <input type="checkbox" defaultChecked />
                                    <span className="checkmark"></span>
                                    <div>
                                        <strong>Weekly Digest</strong>
                                        <p className="checkbox-hint">Receive a weekly summary of reporting activity.</p>
                                    </div>
                                </label>
                            </div>
                            <button className="btn btn-primary mt-4">Save Preferences</button>
                        </div>
                    )}

                    {activeTab === 'system' && user.role === 'admin' && (
                        <div className="settings-section fade-in">
                            <h2>Global Platform Settings</h2>
                            <div className="form-group settings-field">
                                <label>Platform Name</label>
                                <input type="text" className="form-control" defaultValue="SecureReport" />
                            </div>
                            <div className="form-group">
                                <label>Maintenance Mode</label>
                                <select className="form-control settings-field-xs">
                                    <option value="off">Off (Live)</option>
                                    <option value="on">On (Offline to Users)</option>
                                </select>
                            </div>
                            <button className="btn btn-primary mt-4">Save System Settings</button>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default Settings;
