/**
 * Cafe Marahuyo — Inventory Page Script (v2)
 * 
 * Full CRUD for inventory items, stock-in/out, search, filter, export.
 * v2: Expiration dates, integer quantities, Material Icons, updated labels.
 */

// ─── State ───────────────────────────────────────
let currentPage = 1;
let currentSort = 'name';
let currentOrder = 'asc';
let categories = [];
const user = getUser();
const isAdmin = user && user.role === 'admin';

document.addEventListener('DOMContentLoaded', () => {
    if (!requireAuthentication()) return;

    if (isAdmin) {
        const addBtn = document.getElementById('btn-add-item');
        if (addBtn) addBtn.style.display = '';
    }

    loadCategories();
    loadInventory();
    bindEvents();
});

// ─── Load Categories ─────────────────────────────
async function loadCategories() {
    try {
        categories = await apiFetch('/inventory/categories');
        const filterSelect = document.getElementById('filter-category');
        const itemCategory = document.getElementById('item-category');

        categories.forEach(cat => {
            const opt1 = new Option(cat.name, cat.id);
            filterSelect.appendChild(opt1);

            if (itemCategory) {
                const opt2 = new Option(cat.name, cat.id);
                itemCategory.appendChild(opt2);
            }
        });
    } catch (err) {
        console.error('Failed to load categories:', err);
    }
}

// ─── Load Inventory ──────────────────────────────
async function loadInventory() {
    const searchVal = document.getElementById('search-input').value;
    const categoryVal = document.getElementById('filter-category').value;
    const statusVal = document.getElementById('filter-status').value;

    const params = new URLSearchParams({
        page: currentPage,
        limit: 15,
        sort: currentSort,
        order: currentOrder
    });

    if (searchVal) params.set('search', searchVal);
    if (categoryVal !== 'all') params.set('category', categoryVal);
    if (statusVal !== 'all') params.set('status', statusVal);

    try {
        const data = await apiFetch(`/inventory?${params}`);
        renderTable(data.items);
        renderPagination(data.pagination);
    } catch (err) {
        showToast('Failed to load inventory', 'error');
    }
}

// ─── Render Table ────────────────────────────────
function renderTable(items) {
    const tbody = document.getElementById('inventory-body');

    if (items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8">
                    <div class="empty-state">
                        <span class="material-icons-round empty-state-icon">inventory_2</span>
                        <h3>No items found</h3>
                        <p>Try adjusting your search or filters</p>
                    </div>
                </td>
            </tr>
        `;
        return;
    }

    tbody.innerHTML = items.map((item, i) => {
        const status = getStockStatus(item.quantity, item.min_stock_level);
        const expStatus = getExpirationStatus(item.expiration_date);

        // Build expiration cell
        let expCell = '<span class="text-muted">—</span>';
        if (expStatus) {
            if (expStatus.class === 'expired') {
                expCell = `<span class="badge badge-expired"><span class="material-icons-round" style="font-size:14px;">error</span> ${expStatus.label}</span>`;
            } else if (expStatus.class === 'expiring') {
                expCell = `<span class="badge badge-expiring"><span class="material-icons-round" style="font-size:14px;">schedule</span> ${expStatus.label}</span>`;
            } else if (expStatus.class === 'warning') {
                expCell = `<span class="badge badge-warning">${expStatus.label}</span>`;
            } else {
                expCell = `<span class="text-muted" style="font-size:0.82rem;">${expStatus.label}</span>`;
            }
        }
        
        const hasBatches = item.batches && item.batches.length > 0;
        let batchRowsHtml = '';
        
        if (hasBatches) {
            const batchTrs = item.batches.map(b => {
                const bExp = getExpirationStatus(b.expiration_date);
                let bExpCell = '<span class="text-muted">—</span>';
                if (bExp) {
                    if (bExp.class === 'expired') {
                        bExpCell = `<span class="text-danger fw-600">${bExp.label}</span>`;
                    } else if (bExp.class === 'expiring' || bExp.class === 'warning') {
                        bExpCell = `<span class="text-warning fw-600">${bExp.label}</span>`;
                    } else {
                        bExpCell = `<span class="text-muted">${bExp.label}</span>`;
                    }
                }
                
                return `
                    <tr>
                        <td>Batch #${b.id}</td>
                        <td><strong>${formatNumber(b.quantity)}</strong> ${item.unit}</td>
                        <td>${bExpCell}</td>
                    </tr>
                `;
            }).join('');
            
            batchRowsHtml = `
                <tr class="batch-row" id="batch-row-${item.id}">
                    <td colspan="8" class="batch-table-container">
                        <table class="batch-table">
                            <thead>
                                <tr>
                                    <th>Batch ID</th>
                                    <th>Quantity</th>
                                    <th>Expiration Date</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${batchTrs}
                            </tbody>
                        </table>
                    </td>
                </tr>
            `;
        }

        return `
            <tr style="animation: fadeSlideUp 0.3s ease ${i * 0.03}s both;">
                <td>
                    <div class="cell-item">
                        ${hasBatches ? `<button class="expand-btn" onclick="toggleBatchRow(${item.id}, this)" title="View Batches"><span class="material-icons-round">expand_more</span></button>` : `<span style="width:28px"></span>`}
                        <span class="cell-icon">${getCategoryIcon(item.category_icon)}</span>
                        <div>
                            <div class="cell-name">${escapeHtml(item.name)}</div>
                            ${item.description ? `<div class="cell-description">${escapeHtml(item.description)}</div>` : ''}
                        </div>
                    </div>
                </td>
                <td><span class="badge badge-category">${escapeHtml(item.category_name)}</span></td>
                <td>
                    <strong>${formatNumber(item.quantity)}</strong>
                    <div class="stock-bar-container">
                        <div class="stock-bar ${status.barClass}" style="width: ${status.barWidth}%"></div>
                    </div>
                </td>
                <td class="text-muted">${item.unit}</td>
                <td>${formatCurrency(item.cost_per_unit)}</td>
                <td>${expCell}</td>
                <td><span class="badge badge-${status.class}">${status.label}</span></td>
                <td>
                    <div class="cell-actions">
                        <button class="btn btn-ghost btn-icon" title="Add Stocks" onclick="openStockIn(${item.id}, '${escapeAttr(item.name)}', ${item.quantity}, '${item.unit}')">
                            <span class="material-icons-round text-success">add_circle</span>
                        </button>
                        <button class="btn btn-ghost btn-icon" title="Remove Stocks" onclick="openStockOut(${item.id}, '${escapeAttr(item.name)}', ${item.quantity}, '${item.unit}')">
                            <span class="material-icons-round text-danger">remove_circle</span>
                        </button>
                        ${isAdmin ? `
                            <button class="btn btn-ghost btn-icon" title="Edit" onclick="openEditItem(${item.id})">
                                <span class="material-icons-round text-accent">edit</span>
                            </button>
                            <button class="btn btn-ghost btn-icon" title="Delete" onclick="openDeleteItem(${item.id}, '${escapeAttr(item.name)}')">
                                <span class="material-icons-round text-danger">delete</span>
                            </button>
                        ` : ''}
                    </div>
                </td>
            </tr>
            ${batchRowsHtml}
        `;
    }).join('');
}

window.toggleBatchRow = function(itemId, btn) {
    const row = document.getElementById(`batch-row-${itemId}`);
    if (row) {
        row.classList.toggle('expanded');
        btn.classList.toggle('open');
    }
};

// ─── Render Pagination ───────────────────────────
function renderPagination(pagination) {
    const info = document.getElementById('table-info');
    const paginationDiv = document.getElementById('pagination');

    const start = (pagination.page - 1) * pagination.limit + 1;
    const end = Math.min(pagination.page * pagination.limit, pagination.total);
    info.textContent = `Showing ${start}–${end} of ${pagination.total} items`;

    if (pagination.pages <= 1) {
        paginationDiv.innerHTML = '';
        return;
    }

    let html = `<button ${pagination.page <= 1 ? 'disabled' : ''} onclick="goToPage(${pagination.page - 1})"><span class="material-icons-round" style="font-size:16px;">chevron_left</span></button>`;

    for (let p = 1; p <= pagination.pages; p++) {
        if (pagination.pages > 7 && p > 2 && p < pagination.pages - 1 && Math.abs(p - pagination.page) > 1) {
            if (p === 3 || p === pagination.pages - 2) html += `<button disabled>…</button>`;
            continue;
        }
        html += `<button class="${p === pagination.page ? 'active' : ''}" onclick="goToPage(${p})">${p}</button>`;
    }

    html += `<button ${pagination.page >= pagination.pages ? 'disabled' : ''} onclick="goToPage(${pagination.page + 1})"><span class="material-icons-round" style="font-size:16px;">chevron_right</span></button>`;
    paginationDiv.innerHTML = html;
}

function goToPage(page) { currentPage = page; loadInventory(); }

// ─── Event Bindings ──────────────────────────────
function bindEvents() {
    document.getElementById('search-input').addEventListener('input', debounce(() => { currentPage = 1; loadInventory(); }));
    document.getElementById('filter-category').addEventListener('change', () => { currentPage = 1; loadInventory(); });
    document.getElementById('filter-status').addEventListener('change', () => { currentPage = 1; loadInventory(); });

    document.querySelectorAll('.data-table th[data-sort]').forEach(th => {
        th.addEventListener('click', () => {
            const sortKey = th.dataset.sort;
            if (currentSort === sortKey) {
                currentOrder = currentOrder === 'asc' ? 'desc' : 'asc';
            } else {
                currentSort = sortKey;
                currentOrder = 'asc';
            }
            loadInventory();
        });
    });

    document.getElementById('btn-add-item')?.addEventListener('click', openAddItem);
    document.getElementById('item-modal-close')?.addEventListener('click', () => closeModal('item-modal'));
    document.getElementById('item-modal-cancel')?.addEventListener('click', () => closeModal('item-modal'));
    document.getElementById('item-modal-save')?.addEventListener('click', saveItem);
    document.getElementById('stock-in-save')?.addEventListener('click', performStockIn);
    document.getElementById('stock-out-save')?.addEventListener('click', performStockOut);
    document.getElementById('delete-confirm')?.addEventListener('click', performDelete);

    document.getElementById('btn-export-csv')?.addEventListener('click', async () => {
        try {
            const dateFrom = document.getElementById('export-date-from')?.value;
            const dateTo = document.getElementById('export-date-to')?.value;
            let url = '/inventory/export/excel';
            const params = new URLSearchParams();
            if (dateFrom) params.set('date_from', dateFrom);
            if (dateTo) params.set('date_to', dateTo);
            if (params.toString()) url += `?${params.toString()}`;
            
            // To trigger download, we can't just use apiFetch (which expects JSON/text).
            // We need to navigate or use a temporary link with the auth token.
            // But apiFetch handles auth. Let's do a direct fetch and create a blob URL.
            const token = localStorage.getItem('cm_token');
            const res = await fetch(`${API_BASE}${url}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!res.ok) throw new Error('Export failed');
            
            const blob = await res.blob();
            const downloadUrl = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = downloadUrl;
            
            // Extract filename from Content-Disposition if possible
            let filename = 'inventory_export.csv';
            const disposition = res.headers.get('Content-Disposition');
            if (disposition && disposition.indexOf('attachment') !== -1) {
                const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                const matches = filenameRegex.exec(disposition);
                if (matches != null && matches[1]) {
                    filename = matches[1].replace(/['"]/g, '');
                }
            }
            
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(downloadUrl);
            
            showToast('Inventory exported successfully');
        } catch (err) {
            showToast('Failed to export', 'error');
        }
    });
}

// ─── Add Item ────────────────────────────────────
function openAddItem() {
    document.getElementById('item-modal-title').textContent = 'Add New Item';
    document.getElementById('item-id').value = '';
    document.getElementById('item-name').value = '';
    document.getElementById('item-category').value = categories[0]?.id || '';
    document.getElementById('item-unit').value = 'kg';
    document.getElementById('item-quantity').value = '';
    document.getElementById('item-quantity').disabled = false;
    document.getElementById('item-cost').value = '';
    document.getElementById('item-min-stock').value = '5';
    document.getElementById('item-expiration').value = '';
    document.getElementById('item-description').value = '';
    openModal('item-modal');
}

// ─── Edit Item ───────────────────────────────────
async function openEditItem(id) {
    try {
        const item = await apiFetch(`/inventory/${id}`);
        document.getElementById('item-modal-title').textContent = 'Edit Item';
        document.getElementById('item-id').value = item.id;
        document.getElementById('item-name').value = item.name;
        document.getElementById('item-category').value = item.category_id;
        document.getElementById('item-unit').value = item.unit;
        document.getElementById('item-quantity').value = item.quantity;
        document.getElementById('item-quantity').disabled = true;
        document.getElementById('item-cost').value = item.cost_per_unit;
        document.getElementById('item-min-stock').value = item.min_stock_level;
        document.getElementById('item-expiration').value = item.expiration_date || '';
        document.getElementById('item-description').value = item.description || '';
        openModal('item-modal');
    } catch (err) {
        showToast('Failed to load item', 'error');
    }
}

// ─── Save Item ───────────────────────────────────
async function saveItem() {
    const saveBtn = document.getElementById('item-modal-save');
    if (saveBtn && saveBtn.disabled) return;
    if (saveBtn) saveBtn.disabled = true;

    try {
        const id = document.getElementById('item-id').value;
        const payload = {
            name: document.getElementById('item-name').value.trim(),
            category_id: parseInt(document.getElementById('item-category').value),
            unit: document.getElementById('item-unit').value,
            cost_per_unit: parseFloat(document.getElementById('item-cost').value),
            min_stock_level: Math.floor(parseFloat(document.getElementById('item-min-stock').value)) || 5,
            expiration_date: document.getElementById('item-expiration').value || null,
            description: document.getElementById('item-description').value.trim()
        };

        if (!id) {
            payload.quantity = Math.floor(parseFloat(document.getElementById('item-quantity').value)) || 0;
        }

        if (!payload.name || !payload.category_id || isNaN(payload.cost_per_unit)) {
            showToast('Please fill in all required fields', 'warning');
            return;
        }

        if (id) {
            await apiFetch(`/inventory/${id}`, { method: 'PUT', body: payload });
            showToast('Item updated successfully');
        } else {
            await apiFetch('/inventory', { method: 'POST', body: payload });
            showToast('Item added successfully');
        }
        closeModal('item-modal');
        loadInventory();
    } catch (err) {
        showToast(err.message || 'Failed to save item', 'error');
    } finally {
        if (saveBtn) saveBtn.disabled = false;
    }
}

// ─── Stock In/Out ────────────────────────────────
function openStockIn(id, name, currentQty, unit) {
    document.getElementById('stock-in-item-id').value = id;
    document.getElementById('stock-in-item-name').textContent = name;
    document.getElementById('stock-in-current').textContent = `${formatNumber(currentQty)} ${unit}`;
    document.getElementById('stock-in-qty').value = '';
    document.getElementById('stock-in-expiration').value = '';
    document.getElementById('stock-in-notes').value = '';
    openModal('stock-in-modal');
}

function openStockOut(id, name, currentQty, unit) {
    document.getElementById('stock-out-item-id').value = id;
    document.getElementById('stock-out-item-name').textContent = name;
    document.getElementById('stock-out-current').textContent = `${formatNumber(currentQty)} ${unit}`;
    document.getElementById('stock-out-qty').value = '';
    document.getElementById('stock-out-qty').max = currentQty;
    document.getElementById('stock-out-notes').value = '';
    openModal('stock-out-modal');
}

async function performStockIn() {
    const btn = document.getElementById('stock-in-save');
    if (btn && btn.disabled) return;
    if (btn) btn.disabled = true;

    try {
        const id = document.getElementById('stock-in-item-id').value;
        const quantity = Math.floor(parseFloat(document.getElementById('stock-in-qty').value));
        const expiration_date = document.getElementById('stock-in-expiration').value || null;
        const notes = document.getElementById('stock-in-notes').value.trim();

        if (!quantity || quantity <= 0) {
            showToast('Please enter a valid whole number', 'warning');
            return;
        }

        await apiFetch(`/inventory/${id}/stock-in`, {
            method: 'POST',
            body: { quantity, expiration_date, notes }
        });
        showToast('Stock added successfully');
        closeModal('stock-in-modal');
        loadInventory();
    } catch (err) {
        showToast(err.message || 'Stock in failed', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

async function performStockOut() {
    const btn = document.getElementById('stock-out-save');
    if (btn && btn.disabled) return;
    if (btn) btn.disabled = true;

    try {
        const id = document.getElementById('stock-out-item-id').value;
        const quantity = Math.floor(parseFloat(document.getElementById('stock-out-qty').value));
        const notes = document.getElementById('stock-out-notes').value.trim();

        if (!quantity || quantity <= 0) {
            showToast('Please enter a valid whole number', 'warning');
            return;
        }

        await apiFetch(`/inventory/${id}/stock-out`, {
            method: 'POST',
            body: { quantity, notes }
        });
        showToast('Stock removed successfully');
        closeModal('stock-out-modal');
        loadInventory();
    } catch (err) {
        showToast(err.message || 'Stock out failed', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ─── Delete Item ─────────────────────────────────
function openDeleteItem(id, name) {
    document.getElementById('delete-item-id').value = id;
    document.getElementById('delete-item-name').textContent = name;
    openModal('delete-modal');
}

async function performDelete() {
    const btn = document.getElementById('delete-confirm');
    if (btn && btn.disabled) return;
    if (btn) btn.disabled = true;

    try {
        const id = document.getElementById('delete-item-id').value;
        await apiFetch(`/inventory/${id}`, { method: 'DELETE' });
        showToast('Item deleted successfully');
        closeModal('delete-modal');
        loadInventory();
    } catch (err) {
        showToast(err.message || 'Failed to delete item', 'error');
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ─── Helpers ─────────────────────────────────────
function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str || '';
    return div.innerHTML;
}

function escapeAttr(str) {
    return (str || '').replace(/'/g, "\\'").replace(/"/g, '\\"');
}
