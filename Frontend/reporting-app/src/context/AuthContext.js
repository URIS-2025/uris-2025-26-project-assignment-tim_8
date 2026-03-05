import React, { createContext, useState, useContext, useEffect } from 'react';
import { jwtDecode } from 'jwt-decode';

const AuthContext = createContext();

export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);

    const login = (token) => {
        try {
            const decoded = jwtDecode(token);

            // Map the token claims to our user object
            // The Asp.Net Core standard identity claims
            const nameIdentifier = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
            const emailIdentifier = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
            const roleId = decoded['RoleId'] || decoded['role'];

            // We default to some role names or fetch them if needed. 
            // If the token only contains RoleId, we rely on the backend to tell us the string, but for UI purposes we might need to map it or we just store the roleId.
            // Let's store the raw decoded data and the token.
            const userData = {
                id: nameIdentifier,
                email: emailIdentifier,
                name: emailIdentifier, // Since JWT currently only has email, not full name
                roleId: roleId,
                token: token
            };

            setUser(userData);
            localStorage.setItem('authUser', JSON.stringify(userData));
            localStorage.setItem('authToken', token);
        } catch (error) {
            console.error("Failed to decode token during login", error);
        }
    };

    const logout = () => {
        setUser(null);
        localStorage.removeItem('authUser');
        localStorage.removeItem('authToken');
    };

    useEffect(() => {
        const storedUser = localStorage.getItem('authUser');
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
