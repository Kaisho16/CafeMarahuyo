// menu.js

let products = [];
let auditLogs = [];

function initAuth() {
    const token = localStorage.getItem('cm_token');
    const userJson = localStorage.getItem('cm_user');
    
    if (!token || !userJson) {
        window.location.href = '/index.html'; // redirect to login
        return;
    }
    
    const user = JSON.parse(userJson);
    const cashierEl = document.getElementById('cashier-name');
    if (cashierEl) {
        cashierEl.textContent = user.displayName;
    }

    // Only show audit tab for POS managers
    if (user.role === 'admin') {
        const auditBtn = document.getElementById('audit-tab-btn');
        if (auditBtn) auditBtn.style.display = 'block';
    }
}

function logout() {
    localStorage.removeItem('cm_token');
    localStorage.removeItem('cm_user');
    window.location.href = '/index.html';
}

function showToast(msg, isError = false) {
    const container = document.getElementById('toast-container');
    if (!container) return;
    
    const toast = document.createElement('div');
    toast.className = 'toast';
    if (isError) toast.style.borderLeftColor = 'var(--danger)';
    
    toast.innerHTML = `
        <i class="material-icons-round" style="color: ${isError ? 'var(--danger)' : 'var(--success)'}">${isError ? 'error' : 'check_circle'}</i>
        <span>${msg}</span>
    `;
    
    container.appendChild(toast);
    
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(10px)';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

function switchTab(tabId) {
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    
    document.getElementById(tabId).classList.add('active');
    event.currentTarget.classList.add('active');

    if (tabId === 'audit-tab') {
        loadAuditLogs();
    }
}

async function fetchWithAuth(url, options = {}) {
    const token = localStorage.getItem('cm_token');
    options.headers = {
        ...options.headers,
        'Authorization': `Bearer ${token}`
    };
    const res = await fetch(url, options);
    
    if (res.status === 401) {
        logout();
        throw new Error("Unauthorized");
    }
    
    return res;
}

async function loadProducts() {
    try {
        const res = await fetchWithAuth('/api/orders/products');
        if (!res.ok) throw new Error("Failed to fetch products");
        products = await res.json();
        renderProducts();
    } catch (e) {
        console.error(e);
        showToast("Error loading products", true);
    }
}

function renderProducts() {
    const tbody = document.getElementById('menu-list');
    if (!tbody) return;
    
    tbody.innerHTML = '';
    products.forEach(p => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="font-weight: 500;">${p.name}</td>
            <td><span class="badge" style="background:#f1f5f9; color:var(--text-secondary); padding: 4px 8px; border-radius: 4px; font-size: 0.8rem;">${p.categoryName}</span></td>
            <td style="color: var(--accent); font-weight: 600;">₱${p.price.toFixed(2)}</td>
            <td>
                <button class="btn-delete" onclick="removeProduct(${p.id}, '${p.name}')">Remove</button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

async function removeProduct(id, name) {
    if (!confirm(`Are you sure you want to remove '${name}' from the menu?`)) return;
    
    try {
        const res = await fetchWithAuth(`/api/orders/products/${id}`, { method: 'DELETE' });
        if (!res.ok) throw new Error("Failed to remove product");
        
        showToast(`'${name}' removed successfully`);
        loadProducts(); // refresh
    } catch (e) {
        console.error(e);
        showToast("Error removing product", true);
    }
}

document.getElementById('add-product-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const btn = e.target.querySelector('button');
    btn.disabled = true;
    btn.textContent = 'Adding...';

    const req = {
        name: document.getElementById('prod-name').value.trim(),
        categoryName: document.getElementById('prod-category').value,
        price: parseFloat(document.getElementById('prod-price').value),
        imageUrl: null // Can be added later if needed
    };

    try {
        const res = await fetchWithAuth('/api/orders/products', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(req)
        });
        
        if (!res.ok) throw new Error("Failed to add product");
        
        showToast(`'${req.name}' added successfully`);
        e.target.reset();
        loadProducts();
    } catch (err) {
        console.error(err);
        showToast("Error adding product", true);
    } finally {
        btn.disabled = false;
        btn.textContent = 'Add Product';
    }
});

async function loadAuditLogs() {
    try {
        const res = await fetchWithAuth('/api/orders/products/audit');
        if (!res.ok) {
            if (res.status === 403) {
                showToast("Access Denied: POS Manager only", true);
            } else {
                throw new Error("Failed to fetch audit logs");
            }
            return;
        }
        
        auditLogs = await res.json();
        renderAuditLogs();
    } catch (e) {
        console.error(e);
        showToast("Error loading audit logs", true);
    }
}

function renderAuditLogs() {
    const tbody = document.getElementById('audit-list');
    if (!tbody) return;
    
    tbody.innerHTML = '';
    
    if (auditLogs.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: var(--text-muted);">No recent transactions found.</td></tr>`;
        return;
    }
    
    auditLogs.forEach(l => {
        const actionColor = l.action === 'Add' ? 'var(--success)' : 'var(--danger)';
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="color: var(--text-secondary); font-size: 0.9rem;">${l.timestamp}</td>
            <td><span style="color: ${actionColor}; font-weight: 600;">${l.action}</span></td>
            <td style="font-weight: 500;">${l.productName}</td>
            <td>${l.performedBy}</td>
            <td style="color: var(--text-secondary); font-size: 0.9rem;">${l.details || '-'}</td>
        `;
        tbody.appendChild(tr);
    });
}

document.addEventListener('DOMContentLoaded', () => {
    initAuth();
    loadProducts();
});
