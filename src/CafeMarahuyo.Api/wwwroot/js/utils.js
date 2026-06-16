/**
 * Cafe Marahuyo — Shared Utilities (v2)
 * 
 * Common functions for API calls, authentication, toasts, theme toggle, and UI helpers.
 */

// ─── API Configuration ───────────────────────────
const API_BASE = '/api';

// ─── Theme Management ────────────────────────────
// Theme management removed per user request. System defaults to light theme.

// ─── Auth ────────────────────────────────────────

function getToken() {
    return localStorage.getItem('cm_token');
}

function getUser() {
    const data = localStorage.getItem('cm_user');
    return data ? JSON.parse(data) : null;
}

function saveAuth(token, user) {
    localStorage.setItem('cm_token', token);
    localStorage.setItem('cm_user', JSON.stringify(user));
}

function logout() {
    localStorage.removeItem('cm_token');
    localStorage.removeItem('cm_user');
    window.location.href = '/';
}

function requireAuthentication() {
    const token = getToken();
    if (!token) {
        window.location.href = '/';
        return false;
    }
    setupUserUI();
    return true;
}

function setupUserUI() {
    const user = getUser();
    if (!user) return;

    const avatar = document.getElementById('user-avatar');
    const displayName = document.getElementById('user-display-name');
    const role = document.getElementById('user-role');

    if (avatar) avatar.textContent = user.displayName.charAt(0).toUpperCase();
    if (displayName) displayName.textContent = user.displayName;
    const isAdmin = ['admin', 'Inventory Manager', 'POS Manager'].includes(user.role);
    if (role) role.textContent = isAdmin ? 'Inventory Manager' : 'Staff';

    // Show admin-only UI elements
    if (isAdmin) {
        document.querySelectorAll('[data-admin-only]').forEach(el => el.style.display = '');
    }

    // Set up logout
    const logoutBtn = document.getElementById('btn-logout');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', () => logout());
    }
}

// ─── API Fetch ───────────────────────────────────

async function apiFetch(endpoint, options = {}) {
    const token = getToken();
    const config = {
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
            ...(options.headers || {})
        },
        ...options
    };

    if (options.body && typeof options.body === 'object' && !(options.body instanceof FormData)) {
        config.body = JSON.stringify(options.body);
    }

    try {
        const response = await fetch(`${API_BASE}${endpoint}`, config);

        if (response.status === 401) {
            logout();
            return null;
        }

        if (response.headers.get('Content-Type')?.includes('text/csv')) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            let filename = 'export.csv';
            const disposition = response.headers.get('Content-Disposition');
            if (disposition) {
                const utf8Match = disposition.match(/filename\*=UTF-8''([^;]+)/i);
                const standardMatch = disposition.match(/filename="?([^;"]+)"?/i);
                if (utf8Match && utf8Match[1]) {
                    filename = decodeURIComponent(utf8Match[1]);
                } else if (standardMatch && standardMatch[1]) {
                    filename = standardMatch[1];
                }
            }
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            a.remove();
            return { success: true };
        }

        const text = await response.text();
        const data = text ? JSON.parse(text) : {};

        if (!response.ok) {
            throw new Error(data.error || 'Something went wrong');
        }

        return data;
    } catch (err) {
        if (err.message !== 'Failed to fetch') {
            console.error('API Error:', err);
        }
        throw err;
    }
}

// ─── Toast Notifications ─────────────────────────

function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const icons = {
        success: 'check_circle',
        error: 'error',
        warning: 'warning'
    };

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `
        <span class="material-icons-round toast-icon ${type === 'success' ? 'text-success' : type === 'error' ? 'text-danger' : 'text-warning'}">${icons[type] || 'info'}</span>
        <span class="toast-message">${message}</span>
        <button class="toast-close" onclick="this.parentElement.remove()">
            <span class="material-icons-round" style="font-size:18px;">close</span>
        </button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.add('hide');
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

// ─── Modal Helpers ───────────────────────────────

function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.add('show');
        document.body.style.overflow = 'hidden';
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.remove('show');
        document.body.style.overflow = '';
    }
}

function initModals() {
    document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                overlay.classList.remove('show');
                document.body.style.overflow = '';
            }
        });
    });

    document.querySelectorAll('[data-close]').forEach(btn => {
        btn.addEventListener('click', () => closeModal(btn.dataset.close));
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            document.querySelectorAll('.modal-overlay.show').forEach(overlay => {
                overlay.classList.remove('show');
                document.body.style.overflow = '';
            });
        }
    });
}

// ─── Mobile Sidebar ──────────────────────────────

function initSidebar() {
    const toggle = document.getElementById('mobile-toggle');
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebar-overlay');

    if (toggle && sidebar) {
        toggle.addEventListener('click', () => {
            sidebar.classList.toggle('open');
            overlay?.classList.toggle('show');
        });

        overlay?.addEventListener('click', () => {
            sidebar.classList.remove('open');
            overlay.classList.remove('show');
        });
    }
}

// ─── Formatting Helpers ──────────────────────────

function formatCurrency(amount) {
    return '₱' + Number(amount).toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatDate(dateStr) {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-PH', { year: 'numeric', month: 'short', day: 'numeric' });
}

function formatDateTime(dateStr) {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return date.toLocaleString('en-PH', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

/**
 * Format number — always whole numbers now
 */
function formatNumber(num) {
    return Math.floor(Number(num)).toString();
}

function debounce(fn, delay = 300) {
    let timer;
    return function (...args) {
        clearTimeout(timer);
        timer = setTimeout(() => fn.apply(this, args), delay);
    };
}

/**
 * FIXED: Low stock = quantity STRICTLY LESS THAN min level (not <=)
 */
function getStockStatus(quantity, minLevel) {
    const ratio = minLevel > 0 ? quantity / minLevel : 1;
    if (quantity <= 0) return { label: 'Out of Stock', class: 'danger', barClass: 'low', barWidth: 0 };
    if (quantity < minLevel) return { label: 'Low Stock', class: 'danger', barClass: 'low', barWidth: Math.min(ratio * 100, 100) };
    if (ratio < 2) return { label: 'Warning', class: 'warning', barClass: 'warning', barWidth: Math.min(ratio * 50, 100) };
    return { label: 'In Stock', class: 'success', barClass: 'ok', barWidth: 100 };
}

/**
 * Get expiration status for an item
 */
function getExpirationStatus(expirationDate) {
    if (!expirationDate) return null;
    
    const now = new Date();
    now.setHours(0, 0, 0, 0);
    const expDate = new Date(expirationDate);
    expDate.setHours(0, 0, 0, 0);
    
    const diffMs = expDate - now;
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays < 0) return { label: 'Expired', class: 'expired', days: diffDays };
    if (diffDays <= 7) return { label: `${diffDays}d left`, class: 'expiring', days: diffDays };
    if (diffDays <= 30) return { label: `${diffDays}d left`, class: 'warning', days: diffDays };
    return { label: formatDate(expirationDate), class: 'ok', days: diffDays };
}

// ─── Material Icon mapper for categories ─────────
function getCategoryIcon(iconName) {
    return `<span class="material-icons-round" style="font-size:22px;">${iconName || 'inventory_2'}</span>`;
}

// ─── Initialization ──────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    initTheme();
    initSidebar();
    initModals();
});
