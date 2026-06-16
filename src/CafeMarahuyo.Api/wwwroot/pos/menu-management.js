// menu-management.js

let allProducts = [];
let allSizes = [];
let allAddons = [];
let allLogs = [];

function initAuth() {
    const token = localStorage.getItem('cm_token');
    const userJson = localStorage.getItem('cm_user');
    
    if (!token || !userJson) {
        window.location.href = '/index.html';
        return;
    }
    const user = JSON.parse(userJson);
    const isAdmin = ['admin', 'Inventory Manager', 'POS Manager'].includes(user.role);
    
    if (!isAdmin) {
        window.location.href = 'index.html';
        return;
    }
    
    const cashierEl = document.getElementById('cashier-name');
    if (cashierEl) cashierEl.textContent = user.displayName;

    const roleEl = document.getElementById('cashier-role');
    if (roleEl) roleEl.textContent = 'POS Manager';
}

function logout() {
    localStorage.removeItem('cm_token');
    localStorage.removeItem('cm_user');
    window.location.href = '/index.html';
}

function showToast(msg, isError = false) {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = 'toast';
    if (isError) toast.style.borderLeftColor = 'var(--danger)';
    toast.innerHTML = `<i class="material-icons-round" style="color: ${isError ? 'var(--danger)' : 'var(--success)'}">${isError ? 'error' : 'check_circle'}</i><span>${msg}</span>`;
    container.appendChild(toast);
    setTimeout(() => { toast.style.opacity = '0'; toast.style.transform = 'translateY(10px)'; setTimeout(() => toast.remove(), 300); }, 3000);
}

async function fetchWithAuth(url, options = {}) {
    const token = localStorage.getItem('cm_token');
    options.headers = { ...options.headers, 'Authorization': `Bearer ${token}` };
    const res = await fetch(url, options);
    if (res.status === 401) { logout(); throw new Error("Unauthorized"); }
    return res;
}

function switchTab(tabId, el) {
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    el.classList.add('active');
    document.getElementById(`tab-${tabId}`).classList.add('active');
    
    if (tabId === 'products') fetchProducts();
    if (tabId === 'sizes') fetchSizes();
    if (tabId === 'addons') fetchAddons();
    if (tabId === 'settings') fetchSettings();
    if (tabId === 'audit') fetchAuditLogs();
}

// ========================== PRODUCTS ==========================

async function fetchProducts() {
    const res = await fetchWithAuth('/api/orders/products/all');
    if (res.ok) {
        allProducts = await res.json();
        renderProducts();
    }
}

function renderProducts() {
    const query = document.getElementById('prod-search').value.toLowerCase();
    const tbody = document.getElementById('prod-tbody');
    const filtered = allProducts.filter(p => p.name.toLowerCase().includes(query) || p.categoryName.toLowerCase().includes(query));
    
    tbody.innerHTML = filtered.map(p => `
        <tr style="${!p.isAvailable ? 'opacity: 0.6;' : ''}">
            <td style="font-weight:600;">${p.name}</td>
            <td>${p.categoryName}</td>
            <td>₱${p.price.toFixed(2)}</td>
            <td>${p.isAvailable ? '<span class="badge badge-cash">Available</span>' : '<span class="badge" style="background:#e2e8f0;color:#64748b;">Hidden</span>'}</td>
            <td>
                <button class="action-btn" onclick="openProductModal(${p.id})"><i class="material-icons-round">edit</i></button>
                <button class="action-btn delete" onclick="deleteProduct(${p.id})"><i class="material-icons-round">delete</i></button>
            </td>
        </tr>
    `).join('');
}

function openProductModal(id = null) {
    const p = id ? allProducts.find(x => x.id === id) : null;
    document.getElementById('admin-modal-title').textContent = p ? 'Edit Product' : 'New Product';
    
    document.getElementById('admin-modal-body').innerHTML = `
        <div class="form-group">
            <label>Name</label>
            <input type="text" id="m-prod-name" class="form-input" value="${p ? p.name : ''}">
        </div>
        <div class="form-group">
            <label>Category</label>
            <input type="text" id="m-prod-cat" class="form-input" value="${p ? p.categoryName : 'Coffee'}">
        </div>
        <div class="form-group">
            <label>Price (₱)</label>
            <input type="number" id="m-prod-price" class="form-input" value="${p ? p.price : 0}">
        </div>

        <div class="form-group">
            <label>Description</label>
            <textarea id="m-prod-desc" class="form-input" rows="2">${p ? (p.description||'') : ''}</textarea>
        </div>
        ${p ? `
        <div class="form-group" style="flex-direction:row;align-items:center;margin-top:8px;">
            <input type="checkbox" id="m-prod-avail" ${p.isAvailable ? 'checked' : ''} style="width:18px;height:18px;">
            <label style="margin-left:8px;font-size:0.9rem;">Available in POS</label>
        </div>
        ` : ''}
    `;
    
    document.getElementById('admin-save-btn').onclick = () => saveProduct(id);
    document.getElementById('admin-modal-overlay').classList.add('open');
    document.getElementById('admin-modal').classList.add('open');
}

async function saveProduct(id) {
    // Disable save button to prevent double click
    const saveBtn = document.getElementById('admin-save-btn');
    if (saveBtn && saveBtn.disabled) return;
    if (saveBtn) {
        saveBtn.disabled = true;
        saveBtn.textContent = "Saving...";
    }

    try {
        const req = {
            name: document.getElementById('m-prod-name').value,
            categoryName: document.getElementById('m-prod-cat').value,
            price: parseFloat(document.getElementById('m-prod-price').value) || 0,
            imageUrl: null,
            description: document.getElementById('m-prod-desc').value
        };
    
        if (id) {
            req.isAvailable = document.getElementById('m-prod-avail').checked;
            const res = await fetchWithAuth(`/api/orders/products/${id}`, { method: 'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify(req)});
            if (res.ok) { showToast("Updated!"); closeAdminModal(); fetchProducts(); }
        } else {
            const res = await fetchWithAuth(`/api/orders/products`, { method: 'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(req)});
            if (res.ok) { showToast("Created!"); closeAdminModal(); fetchProducts(); }
        }
    } catch (err) {
        showToast(err.message || 'Failed to save product', true);
    } finally {
        saveBtn.disabled = false;
        saveBtn.textContent = "Save";
    }
}

async function deleteProduct(id) {
    if(!confirm("Are you sure you want to remove this product? It will just be hidden from the POS.")) return;
    const res = await fetchWithAuth(`/api/orders/products/${id}`, { method: 'DELETE' });
    if(res.ok) { showToast("Removed!"); fetchProducts(); }
}

// ========================== SIZES ==========================

async function fetchSizes() {
    const res = await fetchWithAuth('/api/orders/size-modifiers/all');
    if (res.ok) {
        allSizes = await res.json();
        document.getElementById('size-tbody').innerHTML = allSizes.map(s => `
            <tr>
                <td style="font-weight:600;">${s.sizeName}</td>
                <td>+₱${s.priceModifier.toFixed(2)}</td>
                <td>
                    <button class="action-btn" onclick="toggleSize(${s.id})" style="color: ${s.isActive ? 'var(--success)' : 'var(--text-muted)'}">
                        <i class="material-icons-round">${s.isActive ? 'toggle_on' : 'toggle_off'}</i>
                    </button>
                </td>
                <td>
                    <button class="action-btn" onclick="openSizeModal(${s.id})"><i class="material-icons-round">edit</i></button>
                </td>
            </tr>
        `).join('');
    }
}

function openSizeModal(id) {
    const s = allSizes.find(x => x.id === id);
    document.getElementById('admin-modal-title').textContent = `Edit Size: ${s.sizeName}`;
    document.getElementById('admin-modal-body').innerHTML = `
        <div class="form-group">
            <label>Price Modifier (+₱)</label>
            <input type="number" id="m-size-price" class="form-input" value="${s.priceModifier}">
        </div>
    `;
    document.getElementById('admin-save-btn').onclick = async () => {
        const btn = document.getElementById('admin-save-btn');
        if (btn.disabled) return;
        btn.disabled = true;
        try {
            const p = parseFloat(document.getElementById('m-size-price').value) || 0;
            const res = await fetchWithAuth(`/api/orders/size-modifiers/${id}`, { method: 'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify({priceModifier: p})});
            if(res.ok) { showToast("Saved"); closeAdminModal(); fetchSizes(); }
        } finally {
            btn.disabled = false;
        }
    };
    document.getElementById('admin-modal-overlay').classList.add('open');
    document.getElementById('admin-modal').classList.add('open');
}

async function toggleSize(id) {
    const res = await fetchWithAuth(`/api/orders/size-modifiers/${id}/toggle`, { method: 'PATCH' });
    if(res.ok) fetchSizes();
}

// ========================== ADDONS ==========================

async function fetchAddons() {
    const res = await fetchWithAuth('/api/orders/addons/all');
    if (res.ok) {
        allAddons = await res.json();
        document.getElementById('addon-tbody').innerHTML = allAddons.map(a => `
            <tr>
                <td style="text-transform:capitalize;">${a.category}</td>
                <td style="font-weight:600;">${a.name}</td>
                <td>+₱${a.price.toFixed(2)}</td>
                <td>
                    <button class="action-btn" onclick="toggleAddon(${a.id})" style="color: ${a.isActive ? 'var(--success)' : 'var(--text-muted)'}">
                        <i class="material-icons-round">${a.isActive ? 'toggle_on' : 'toggle_off'}</i>
                    </button>
                </td>
                <td>
                    <button class="action-btn" onclick="openAddonModal(${a.id})"><i class="material-icons-round">edit</i></button>
                    <button class="action-btn delete" onclick="deleteAddon(${a.id})"><i class="material-icons-round">delete</i></button>
                </td>
            </tr>
        `).join('');
    }
}

function openAddonModal(id = null) {
    const a = id ? allAddons.find(x => x.id === id) : null;
    document.getElementById('admin-modal-title').textContent = a ? 'Edit Add-on' : 'New Add-on';
    document.getElementById('admin-modal-body').innerHTML = `
        <div class="form-group">
            <label>Category (milk, syrup, shot)</label>
            <input type="text" id="m-addon-cat" class="form-input" value="${a ? a.category : 'syrup'}">
        </div>
        <div class="form-group">
            <label>Name</label>
            <input type="text" id="m-addon-name" class="form-input" value="${a ? a.name : ''}">
        </div>
        <div class="form-group">
            <label>Price (+₱)</label>
            <input type="number" id="m-addon-price" class="form-input" value="${a ? a.price : 0}">
        </div>
    `;
    document.getElementById('admin-save-btn').onclick = async () => {
        const btn = document.getElementById('admin-save-btn');
        if (btn.disabled) return;
        btn.disabled = true;
        try {
            const req = {
                category: document.getElementById('m-addon-cat').value,
                name: document.getElementById('m-addon-name').value,
                price: parseFloat(document.getElementById('m-addon-price').value) || 0
            };
            const method = id ? 'PUT' : 'POST';
            const url = id ? `/api/orders/addons/${id}` : `/api/orders/addons`;
            const res = await fetchWithAuth(url, { method, headers:{'Content-Type':'application/json'}, body: JSON.stringify(req)});
            if(res.ok) { showToast("Saved"); closeAdminModal(); fetchAddons(); }
        } finally {
            btn.disabled = false;
        }
    };
    document.getElementById('admin-modal-overlay').classList.add('open');
    document.getElementById('admin-modal').classList.add('open');
}

async function toggleAddon(id) {
    const res = await fetchWithAuth(`/api/orders/addons/${id}/toggle`, { method: 'PATCH' });
    if(res.ok) fetchAddons();
}
async function deleteAddon(id) {
    if(!confirm("Delete this add-on permanently?")) return;
    const res = await fetchWithAuth(`/api/orders/addons/${id}`, { method: 'DELETE' });
    if(res.ok) { showToast("Deleted"); fetchAddons(); }
}

// ========================== SETTINGS ==========================
async function fetchSettings() {
    const res = await fetchWithAuth('/api/orders/settings');
    if (res.ok) {
        const data = await res.json();
        document.getElementById('set-tax').value = data.taxRate;
        document.getElementById('set-footer').value = data.receiptFooter;
    }
}

async function saveSettings() {
    const btn = document.querySelector('button[onclick="saveSettings()"]');
    if (btn && btn.disabled) return;
    if (btn) btn.disabled = true;
    try {
        const req = {
            taxRate: parseFloat(document.getElementById('set-tax').value) || 0,
            receiptFooter: document.getElementById('set-footer').value
        };
        const res = await fetchWithAuth('/api/orders/settings', { method: 'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify(req) });
        if (res.ok) showToast("Settings saved!");
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ========================== AUDIT LOGS ==========================
async function fetchAuditLogs() {
    const res = await fetchWithAuth('/api/orders/products/audit');
    if (res.ok) {
        allLogs = await res.json();
        document.getElementById('audit-tbody').innerHTML = allLogs.map(l => `
            <tr>
                <td style="color:var(--text-secondary);font-size:0.9rem;">${l.timestamp}</td>
                <td><span class="badge badge-cash">${l.performedBy}</span></td>
                <td style="font-weight:600;">${l.action}</td>
                <td>${l.productName}</td>
                <td style="font-size:0.9rem;">${l.details}</td>
            </tr>
        `).join('');
    }
}

function closeAdminModal() {
    document.getElementById('admin-modal-overlay').classList.remove('open');
    document.getElementById('admin-modal').classList.remove('open');
}

document.addEventListener('DOMContentLoaded', () => {
    initAuth();
    fetchProducts();
});
