import React, { useState, useEffect, useRef } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Mail, Lock, User, ArrowRight, Building2, Shield, AlertCircle } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { UserService } from '../services/userService';
import { UserRoleService } from '../services/userRoleService';
import { OrganizationService } from '../services/organizationService';
import { validateEmail, validatePassword } from '../utils/validation';
import { checkPwned } from '../services/pwnedService';
import TurnstileWidget from '../components/TurnstileWidget';
import './Login.css';

const InputField = ({ icon: Icon, type, placeholder, name, required = true, minLength, maxLength, pattern }) => (
    <div className="input-group">
        <Icon className="input-icon" size={20} />
        <input
            type={type}
            name={name}
            placeholder={placeholder}
            className="input-field glass-panel"
            required={required}
            minLength={minLength}
            maxLength={maxLength}
            pattern={pattern}
        />
    </div>
);

const SelectField = ({ icon: Icon, name, value, onChange, options, placeholder, required = true }) => (
    <div className="input-group">
        <Icon className="input-icon" size={20} />
        <select
            name={name}
            value={value}
            onChange={onChange}
            className="input-field glass-panel"
            required={required}
            style={{ color: value ? '#fff' : 'var(--text-muted)' }}
        >
            <option value="" disabled>{placeholder}</option>
            {options.map((opt) => (
                <option key={opt.id} value={opt.id}>
                    {opt.name || opt.title || opt.id}
                </option>
            ))}
        </select>
    </div>
);

const Login = () => {
    const location = useLocation();
    const navigate = useNavigate();
    const { login } = useAuth();
    const queryParams = new URLSearchParams(location.search);
    const initialMode = queryParams.get('mode') === 'signup' ? 'signup' : 'login';

    const [mode, setMode] = useState(initialMode);
    const [error, setError] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [captchaToken, setCaptchaToken] = useState('');
    const captchaRef = useRef(null);

    const [roles, setRoles] = useState([]);
    const [organizations, setOrganizations] = useState([]);
    const [selectedRoleId, setSelectedRoleId] = useState('');
    const [selectedOrgId, setSelectedOrgId] = useState('');
    const [isManagerRoleSelected, setIsManagerRoleSelected] = useState(false);

    useEffect(() => {
        setMode(initialMode);
    }, [initialMode]);

    useEffect(() => {
        const fetchData = async () => {
            try {
                const fetchedRoles = await UserRoleService.getAll();
                const excludedRoles = ['AnonymousUser', 'Reporter'];
                const baseRoles = fetchedRoles.filter(r =>
                    !excludedRoles.includes(r.name) && !excludedRoles.includes(r.title));
                const targetRoles = ['Admin', 'Manager', 'BillingManager'];
                const filteredRoles = baseRoles.filter(r =>
                    targetRoles.includes(r.name) || targetRoles.includes(r.title));
                setRoles(filteredRoles.length > 0 ? filteredRoles : baseRoles);

                const fetchedOrgs = await OrganizationService.getAll();
                setOrganizations(fetchedOrgs);
            } catch (err) {
                console.error('Failed to fetch roles or organizations:', err);
            }
        };
        fetchData();
    }, []);

    const toggleMode = () => {
        setMode(mode === 'login' ? 'signup' : 'login');
        setError('');
    };

    const handleRoleChange = (e) => {
        const newRoleId = e.target.value;
        setSelectedRoleId(newRoleId);
        const selectedRoleObj = roles.find(r => String(r.id) === String(newRoleId));
        const roleName = (selectedRoleObj?.name || selectedRoleObj?.title || '').toLowerCase();
        if (roleName === 'manager') {
            setIsManagerRoleSelected(true);
        } else {
            setIsManagerRoleSelected(false);
            setSelectedOrgId('');
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');

        const email = e.target.email.value.toLowerCase().trim();
        const password = e.target.password.value;

        if (!captchaToken) {
            setError('Please complete the captcha.');
            return;
        }

        try {
            if (mode === 'signup') {
                const fullName = e.target.name ? e.target.name.value : '';
                const nameParts = fullName.split(' ');
                const name = nameParts[0] || 'User';
                const surname = nameParts.slice(1).join(' ') || 'Name';

                const emailRes = validateEmail(email);
                if (!emailRes.valid) {
                    setError(emailRes.message);
                    return;
                }
                const pwRes = validatePassword(password);
                if (!pwRes.valid) {
                    setError(pwRes.message);
                    return;
                }

                if (!selectedRoleId) {
                    setError('Please select a role.');
                    return;
                }
                if (isManagerRoleSelected && !selectedOrgId) {
                    setError('Please select an organization for the Manager role.');
                    return;
                }

                const pwned = await checkPwned(password);
                if (pwned === 'breached') {
                    setError('This password has appeared in a data breach. Please choose another.');
                    return;
                }

                setIsSubmitting(true);
                await UserService.create({
                    name,
                    surname,
                    email,
                    password,
                    username: email,
                    roleId: selectedRoleId,
                    organizationId: isManagerRoleSelected ? selectedOrgId : null,
                    captchaToken,
                });

                setMode('login');
                setError('');
                setSelectedRoleId('');
                setSelectedOrgId('');
                setIsManagerRoleSelected(false);
                // The signup token is now spent; clear it so the login step gets a fresh one.
                setCaptchaToken('');
                captchaRef.current?.reset();
            } else {
                if (!email || !password) {
                    setError('Please enter your email and password.');
                    return;
                }

                setIsSubmitting(true);
                const loginResponse = await UserService.login({ username: email, password, captchaToken });
                const userData = await login(loginResponse);

                if (userData?.role === 'billingmanager') {
                    navigate('/admin/billing');
                } else {
                    navigate('/admin/dashboard');
                }
            }
        } catch (err) {
            setError(err.message || 'An error occurred. Please try again.');
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
                    <h2>{mode === 'login' ? 'Welcome Back' : 'Create an Account'}</h2>
                    <p>{mode === 'login'
                        ? 'Sign in to access your dashboard'
                        : 'Join SecureReport to start submitting securely'}
                    </p>
                </div>

                {error && (
                    <div className="form-alert">
                        <AlertCircle size={16} />
                        <span>{error}</span>
                    </div>
                )}

                <form className="auth-form" onSubmit={handleSubmit}>
                    {mode === 'signup' && (
                        <>
                            <InputField icon={User} type="text" name="name" placeholder="Full Name" required={false} />
                            <SelectField
                                icon={Shield}
                                name="role"
                                value={selectedRoleId}
                                onChange={handleRoleChange}
                                options={roles}
                                placeholder="Select a Role"
                            />
                            {isManagerRoleSelected && (
                                <SelectField
                                    icon={Building2}
                                    name="organization"
                                    value={selectedOrgId}
                                    onChange={(e) => setSelectedOrgId(e.target.value)}
                                    options={organizations}
                                    placeholder="Select Organization"
                                />
                            )}
                        </>
                    )}

                    <InputField icon={Mail} type="email" name="email" placeholder="Email Address" />
                    <InputField
                        icon={Lock}
                        type="password"
                        name="password"
                        placeholder="Password"
                        minLength={mode === 'signup' ? 8 : undefined}
                    />

                    {mode === 'signup' && (
                        <p className="auth-hint">
                            Min. 8 characters with uppercase, lowercase, a number, and a special character.
                        </p>
                    )}

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
                        {isSubmitting
                            ? (mode === 'login' ? 'Signing in...' : 'Creating...')
                            : (mode === 'login' ? 'Sign In' : 'Create Account')}
                        {!isSubmitting && <ArrowRight size={18} />}
                    </button>
                </form>

                <div className="auth-footer">
                    <p>
                        {mode === 'login' ? "Don't have an account? " : 'Already have an account? '}
                        <button className="switch-mode-btn" type="button" onClick={toggleMode}>
                            {mode === 'login' ? 'Sign up' : 'Sign in'}
                        </button>
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

export default Login;
