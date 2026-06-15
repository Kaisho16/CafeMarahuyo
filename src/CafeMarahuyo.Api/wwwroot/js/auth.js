/**
 * Cafe Marahuyo — Login Page Script (v2)
 * 
 * Handles login form submission and authentication.
 * Uses window.currentLoginMode (set by inline script in index.html) to determine
 * whether to redirect to the POS terminal or the inventory dashboard after login.
 */

document.addEventListener('DOMContentLoaded', () => {
    const token = getToken();
    if (token) {
        // If already logged in, redirect based on saved login mode
        const savedMode = localStorage.getItem('cm_login_mode');
        if (savedMode === 'pos') {
            window.location.href = '/pos/index.html';
        } else {
            window.location.href = '/dashboard.html';
        }
        return;
    }

    const form = document.getElementById('login-form');
    const errorDiv = document.getElementById('login-error');
    const loginBtn = document.getElementById('btn-login');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value;

        if (!username || !password) {
            showLoginError('Please enter both username and password.');
            return;
        }

        // Disable button and show loading
        loginBtn.disabled = true;
        loginBtn.innerHTML = '<span class="spinner"></span> Signing in...';
        errorDiv.classList.remove('show');

        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });

            const text = await response.text();
            let data = {};
            try {
                if (text) data = JSON.parse(text);
            } catch(e) {
                console.error("JSON parse error on text:", text);
                throw new Error("Server returned invalid response: " + text);
            }

            if (!response.ok) {
                throw new Error(data.error || 'Login failed');
            }

            // Save auth data
            saveAuth(data.token, data.user);

            // Read the current login mode and save it for future auto-redirects
            const mode = window.currentLoginMode || 'inventory';
            localStorage.setItem('cm_login_mode', mode);
            
            // Redirect based on the selected tab
            if (mode === 'pos') {
                window.location.href = '/pos/index.html';
            } else {
                window.location.href = '/dashboard.html';
            }

        } catch (err) {
            showLoginError(err.message || 'Invalid credentials. Please try again.');
            loginBtn.disabled = false;
            loginBtn.innerHTML = '<span class="material-icons-round">login</span> Sign In';
        }
    });

    function showLoginError(message) {
        errorDiv.textContent = message;
        errorDiv.classList.add('show');
    }
});
