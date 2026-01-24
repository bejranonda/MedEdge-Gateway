// Authentication helper for reading credentials from environment configuration
// This file is used by the Blazor authentication service
// Credentials are injected during Docker build via config-env.js from .env file

window.MedEdgeAuth = {
    // Get credentials from injected environment configuration
    // config-env.js is injected during Docker build from DASHBOARD_USERNAME and DASHBOARD_PASSWORD
    getCredentials: function() {
        if (window.MedEdgeConfig && window.MedEdgeConfig.dashboardUsername) {
            return {
                username: window.MedEdgeConfig.dashboardUsername,
                password: window.MedEdgeConfig.dashboardPassword
            };
        }

        // No credentials configured - authentication will fail
        console.error('Authentication credentials not configured. Please set DASHBOARD_USERNAME and DASHBOARD_PASSWORD environment variables.');
        return {
            username: null,
            password: null
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

        if (!creds.username || !creds.password) {
            return {
                valid: false,
                username: null,
                error: 'Authentication credentials not configured'
            };
        }

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
