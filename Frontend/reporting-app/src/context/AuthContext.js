import React, { createContext, useState, useContext, useEffect } from 'react';

const AuthContext = createContext();

export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }) => {
    // Mock initial state. Real app would check localStorage or a token.
    const [user, setUser] = useState(null);

    const login = (role) => {
        // Mock user objects based on role chosen
        let mockUser = null;
        if (role === 'admin') {
            mockUser = { id: 'U-001', name: 'Eleanor SystemAdmin', email: 'eleanor@platform.com', role: 'admin' };
        } else if (role === 'manager') {
            mockUser = { id: 'U-002', name: 'Alice Walker', email: 'alice@techcorp.com', role: 'manager', orgId: 'ORG-001' };
        } else if (role === 'billing') {
            mockUser = { id: 'U-004', name: 'Charlie Davis', email: 'charlie@designstudio.com', role: 'billing', orgId: 'ORG-002' };
        }
        setUser(mockUser);
        localStorage.setItem('mockUser', JSON.stringify(mockUser));
    };

    const logout = () => {
        setUser(null);
        localStorage.removeItem('mockUser');
    };

    useEffect(() => {
        const storedUser = localStorage.getItem('mockUser');
        if (storedUser) {
            try {
                setUser(JSON.parse(storedUser));
            } catch (e) {
                console.error('Failed to parse user', e);
            }
        }
    }, []);

    return (
        <AuthContext.Provider value={{ user, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};
