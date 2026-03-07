import React, { createContext, useState, useContext, useEffect } from 'react';
import { jwtDecode } from 'jwt-decode';
import { UserRoleService } from '../services/userRoleService';

const AuthContext = createContext();

export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);

    const login = async (token) => {
        try {
            const decoded = jwtDecode(token);

            // Map the token claims to our user object
            const nameIdentifier = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
            const emailIdentifier = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
            const roleId = decoded['RoleId'] || decoded['role'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

            let roleName = 'user';
            if (roleId) {
                try {
                    const roleData = await UserRoleService.getById(roleId);
                    roleName = roleData.title ? roleData.title.toLowerCase() : 'user';
                } catch (err) {
                    console.error("Failed to fetch role name during login", err);
                }
            }

            const userData = {
                id: nameIdentifier,
                email: emailIdentifier,
                name: emailIdentifier,
                roleId: roleId,
                role: roleName, // Store the actual role name for ProtectedRoute
                token: token
            };

            setUser(userData);
            localStorage.setItem('authUser', JSON.stringify(userData));
            localStorage.setItem('authToken', token);

            return userData;
        } catch (error) {
            console.error("Failed to decode token during login", error);
            throw error;
        }
    };

    const logout = () => {
        setUser(null);
        localStorage.removeItem('authUser');
        localStorage.removeItem('authToken');
    };

    useEffect(() => {
        const initializeAuth = async () => {
            const storedUser = localStorage.getItem('authUser');
            if (storedUser) {
                try {
                    const parsedUser = JSON.parse(storedUser);

                    // If the stored user is missing the 'role' field, fetch it now
                    if (parsedUser.roleId && !parsedUser.role) {
                        try {
                            const roleData = await UserRoleService.getById(parsedUser.roleId);
                            parsedUser.role = roleData.title ? roleData.title.toLowerCase() : 'user';
                            localStorage.setItem('authUser', JSON.stringify(parsedUser));
                        } catch (err) {
                            console.error("Failed to fetch role during rehydration", err);
                            parsedUser.role = 'user'; // default fallback
                        }
                    }

                    setUser(parsedUser);
                } catch (e) {
                    console.error('Failed to parse user', e);
                }
            }
        };

        initializeAuth();
    }, []);

    return (
        <AuthContext.Provider value={{ user, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};
