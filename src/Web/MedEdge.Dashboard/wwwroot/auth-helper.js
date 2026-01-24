// Authentication helper for reading credentials from environment configuration
// This file is used by the Blazor authentication service

window.MedEdgeAuth = {
    // Get credentials from injected environment configuration
    getCredentials: function() {
        // config-env.js is injected during Docker build and contains:
        // window.MedEdgeConfig = {
        //   dashboardUsername: 'guest',
        //   dashboardPassword: 'EuT+8Ww7'
        // };

        if (window.MedEdgeConfig && window.MedEdgeConfig.dashboardUsername) {
            return {
                username: window.MedEdgeConfig.dashboardUsername,
                password: window.MedEdgeConfig.dashboardPassword
            };
        }

        // Fallback to environment variables (if running in non-Docker environment)
        // This won't work directly in browser JS, but provides a hook for server-side rendering
        return {
            username: 'guest',
            password: 'EuT+8Ww7'  // Default fallback - should be overridden by env
        };
    },

    // Hash password using SHA-256 (matches C# implementation)
    hashPassword: async function(password) {
        const encoder = new TextEncoder();
        const data = encoder.encode(password + 'MedEdge_Salt_2024');
        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
        return hashHex.substring(0, 32);
    },

    // Validate credentials
    validateCredentials: async function(username, password) {
        const creds = this.getCredentials();
        const passwordHash = await this.hashPassword(password);
        const validHash = await this.hashPassword(creds.password);

        return {
            valid: username.toLowerCase() === creds.username.toLowerCase() &&
                   passwordHash === validHash,
            username: creds.username
        };
    },

    // Save authentication session to sessionStorage
    saveSession: function(username, token) {
        try {
            const session = {
                username: username,
                token: token,
                timestamp: Date.now()
            };
            sessionStorage.setItem('mededge_auth_session', JSON.stringify(session));
            return true;
        } catch (e) {
            console.error('Failed to save session:', e);
            return false;
        }
    },

    // Load authentication session from sessionStorage
    loadSession: function() {
        try {
            const sessionJson = sessionStorage.getItem('mededge_auth_session');
            if (sessionJson) {
                return JSON.parse(sessionJson);
            }
        } catch (e) {
            console.error('Failed to load session:', e);
        }
        return null;
    },

    // Clear authentication session
    clearSession: function() {
        try {
            sessionStorage.removeItem('mededge_auth_session');
            return true;
        } catch (e) {
            console.error('Failed to clear session:', e);
            return false;
        }
    }
};
