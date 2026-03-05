import React, { useState, useEffect } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Mail, Lock, User, ArrowRight, Building2, Shield } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { UserService } from '../services/userService';
import { UserRoleService } from '../services/userRoleService';
import { OrganizationService } from '../services/organizationService';
import './Login.css';

const InputField = ({ icon: Icon, type, placeholder, name, required = true }) => (
    <div className="input-group">
        <Icon className="input-icon" size={20} />
        <input
            type={type}
            name={name}
            placeholder={placeholder}
            className="input-field glass-panel"
            required={required}
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
            style={{ appearance: 'none', backgroundColor: 'transparent', color: value ? 'white' : '#9ca3af' }}
        >
            <option value="" disabled className="text-gray-900">{placeholder}</option>
            {options.map((opt) => (
                <option key={opt.id} value={opt.id} className="text-gray-900">
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

    // Data for dropdowns
    const [roles, setRoles] = useState([]);
    const [organizations, setOrganizations] = useState([]);

    // Controlled inputs for dropdowns
    const [selectedRoleId, setSelectedRoleId] = useState('');
    const [selectedOrgId, setSelectedOrgId] = useState('');
    const [isManagerRoleSelected, setIsManagerRoleSelected] = useState(false);

    // Sync mode with URL if user navigates back/forward
    useEffect(() => {
        setMode(initialMode);
    }, [initialMode]);

    // Fetch Roles and Organizations on mount
    useEffect(() => {
        const fetchData = async () => {
            try {
                const fetchedRoles = await UserRoleService.getAll();
                // Filter to only wanted roles or use all
                const targetRoles = ['Admin', 'Manager', 'BillingManager'];
                const filteredRoles = fetchedRoles.filter(r => targetRoles.includes(r.name));
                setRoles(filteredRoles.length > 0 ? filteredRoles : fetchedRoles);

                const fetchedOrgs = await OrganizationService.getAll();
                setOrganizations(fetchedOrgs);
            } catch (error) {
                console.error("Failed to fetch roles or organizations:", error);
            }
        };
        fetchData();
    }, []);

    const toggleMode = () => {
        setMode(mode === 'login' ? 'signup' : 'login');
    };

    const handleRoleChange = (e) => {
        const newRoleId = e.target.value;
        setSelectedRoleId(newRoleId);

        const selectedRoleObj = roles.find(r => String(r.id) === String(newRoleId));
        const roleName = selectedRoleObj?.name?.toLowerCase() || '';

        if (roleName === 'manager') {
            setIsManagerRoleSelected(true);
        } else {
            setIsManagerRoleSelected(false);
            setSelectedOrgId(''); // Clear org selection if not manager
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        const email = e.target.email.value.toLowerCase();
        const password = e.target.password.value;

        try {
            if (mode === 'signup') {
                const fullName = e.target.name ? e.target.name.value : '';
                const nameParts = fullName.split(' ');
                const name = nameParts[0] || 'User';
                const surname = nameParts.slice(1).join(' ') || 'Name';

                if (!selectedRoleId) {
                    alert("Please select a role.");
                    return;
                }

                if (isManagerRoleSelected && !selectedOrgId) {
                    alert("Please select an organization for the Manager role.");
                    return;
                }

                const newUser = {
                    name,
                    surname,
                    email,
                    password,
                    username: email,
                    roleId: selectedRoleId,
                    organizationId: isManagerRoleSelected ? selectedOrgId : null
                };

                await UserService.create(newUser);
                alert("Account created successfully! Please log in.");
                setMode('login');
                setSelectedRoleId('');
                setSelectedOrgId('');
                setIsManagerRoleSelected(false);
            } else {
                const tokenResponse = await UserService.login({
                    username: email,
                    password
                });

                // The token is a string directly from the modified UserService
                const token = typeof tokenResponse === 'string' ? tokenResponse : tokenResponse.token;

                if (!token) {
                    throw new Error("No token received from the server");
                }

                login(token);
                navigate('/admin/dashboard');
            }
        } catch (error) {
            console.error(error);
            alert(error.message || "An error occurred");
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

                <form className="auth-form" onSubmit={handleSubmit}>
                    {mode === 'signup' && (
                        <>
                            <InputField
                                icon={User}
                                type="text"
                                name="name"
                                placeholder="Full Name (Optional for reporters)"
                                required={false}
                            />

                            <SelectField
                                icon={Shield}
                                name="role"
                                value={selectedRoleId}
                                onChange={handleRoleChange}
                                options={roles}
                                placeholder="Select a Role"
                                required={true}
                            />

                            {isManagerRoleSelected && (
                                <SelectField
                                    icon={Building2}
                                    name="organization"
                                    value={selectedOrgId}
                                    onChange={(e) => setSelectedOrgId(e.target.value)}
                                    options={organizations}
                                    placeholder="Select Organization"
                                    required={true}
                                />
                            )}
                        </>
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

                    {/* Removed mock logic helper text */}

                    <button type="submit" className="btn btn-primary btn-full bounce-hover">
                        {mode === 'login' ? 'Sign In' : 'Create Account'} <ArrowRight size={18} />
                    </button>
                </form>

                <div className="auth-footer">
                    <p>
                        {mode === 'login' ? "Don't have an account? " : "Already have an account? "}
                        <button className="switch-mode-btn" type="button" onClick={toggleMode}>
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
