import React, { createContext, useState, useContext, useEffect } from 'react';
import { jwtDecode } from 'jwt-decode';
import { UserRoleService } from '../services/userRoleService';

const API_BASE_URL = 'http://127.0.0.1:80';

const AuthContext = createContext();

export const useAuth = () => useContext(AuthContext);

const isTokenExpired = (token) => {
    try {
        const { exp } = jwtDecode(token);
        return exp * 1000 < Date.now();
    } catch {
        return true;
    }
};

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    // True until the initial rehydration from storage finishes. Lets route guards
    // distinguish "still restoring the session" from "genuinely logged out", so a
    // page reload no longer bounces an authenticated user to /login.
    const [initializing, setInitializing] = useState(true);

    const login = async (loginResponse) => {
        const accessToken = loginResponse?.accessToken ?? loginResponse;
        const refreshToken = loginResponse?.refreshToken ?? null;

        try {
            const decoded = jwtDecode(accessToken);

            const nameIdentifier = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
            const emailIdentifier =
                decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
                decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
            const roleId = decoded['RoleId'] || decoded['role'] ||
                decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

            let roleName = 'user';
            if (roleId) {
                try {
                    const roleData = await UserRoleService.getById(roleId);
                    roleName = roleData.title ? roleData.title.toLowerCase() : 'user';
                } catch {
                    // default to 'user' if role fetch fails
                }
            }

            const userData = {
                id: nameIdentifier,
                email: emailIdentifier,
                name: emailIdentifier,
                roleId,
                role: roleName,
                token: accessToken,
                refreshToken,
            };

            setUser(userData);
            localStorage.setItem('authUser', JSON.stringify(userData));
            localStorage.setItem('authToken', accessToken);

            return userData;
        } catch (error) {
            throw error;
        }
    };

    const logout = () => {
        setUser(null);
        localStorage.removeItem('authUser');
        localStorage.removeItem('authToken');
    };

    const silentRefresh = async (storedUser) => {
        if (!storedUser.refreshToken) return null;

        const isAnonymous = !storedUser.roleId;
        const endpoint = isAnonymous
            ? `${API_BASE_URL}/api/AnonymousUser/refresh`
            : `${API_BASE_URL}/api/User/refresh`;

        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: storedUser.refreshToken }),
            });

            if (!response.ok) return null;

            const newTokens = await response.json();
            const updated = {
                ...storedUser,
                token: newTokens.accessToken,
                refreshToken: newTokens.refreshToken,
            };
            setUser(updated);
            localStorage.setItem('authUser', JSON.stringify(updated));
            localStorage.setItem('authToken', newTokens.accessToken);
            return updated;
        } catch {
            return null;
        }
    };

    useEffect(() => {
        const initializeAuth = async () => {
            try {
                const storedUser = localStorage.getItem('authUser');
                if (!storedUser) return;

                const parsedUser = JSON.parse(storedUser);

                if (isTokenExpired(parsedUser.token)) {
                    const refreshed = await silentRefresh(parsedUser);
                    if (!refreshed) {
                        logout();
                        return;
                    }
                } else {
                    if (parsedUser.roleId && !parsedUser.role) {
                        try {
                            const roleData = await UserRoleService.getById(parsedUser.roleId);
                            parsedUser.role = roleData.title ? roleData.title.toLowerCase() : 'user';
                            localStorage.setItem('authUser', JSON.stringify(parsedUser));
                        } catch {
                            parsedUser.role = 'user';
                        }
                    }
                    setUser(parsedUser);
                }
            } catch {
                logout();
            } finally {
                setInitializing(false);
            }
        };

        initializeAuth();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return (
        <AuthContext.Provider value={{ user, login, logout, initializing }}>
            {children}
        </AuthContext.Provider>
    );
};
