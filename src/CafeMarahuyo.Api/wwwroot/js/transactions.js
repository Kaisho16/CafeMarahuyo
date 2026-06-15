/**
 * Cafe Marahuyo — Transactions Page Script (v2)
 * 
 * Displays and filters transaction logs with export.
 * v2: Material Icons, integer quantities.
 */

// ─── State ───────────────────────────────────────
let currentPage = 1;

document.addEventListener('DOMContentLoaded', () => {
    if (!requireAuthentication()) return;

    loadCategories();
    setDefaultDateRange();
    loadTransactions();
    bindEvents();
});

// ─── Load Categories ─────────────────────────────
async function loadCategories() {
    try {
        const categories = await apiFetch('/inventory/categories');
        const filterSelect = document.getElementById('filter-category');

        categories.forEach(cat => {
            const opt = new Option(cat.name, cat.id);
            filterSelect.appendChild(opt);
        });
    } catch (err) {
        console.error('Failed to load categories:', err);
    }
}

// ─── Default Date Range (last 7 days) ────────────
function setDefaultDateRange() {
    const today = new Date();
    const weekAgo = new Date();
    weekAgo.setDate(today.getDate() - 7);

    const formatDateLocal = (d) => {
        const yr = d.getFullYear();
        const mo = String(d.getMonth() + 1).padStart(2, '0');
        const da = String(d.getDate()).padStart(2, '0');
        return `${yr}-${mo}-${da}`;
    };

    document.getElementById('filter-date-to').value = formatDateLocal(today);
    document.getElementById('filter-date-from').value = formatDateLocal(weekAgo);
}

// ─── Load Transactions ──────────────────────────
async function loadTransactions() {
    const dateFrom = document.getElementById('filter-date-from').value;
    const dateTo = document.getElementById('filter-date-to').value;
    const category = document.getElementById('filter-category').value;
    const type = document.getElementById('filter-type').value;

    const params = new URLSearchParams({ page: currentPage, limit: 20 });

    if (dateFrom) params.set('date_from', dateFrom);
    if (dateTo) params.set('date_to', dateTo);
    if (category !== 'all') params.set('category', category);
    if (type !== 'all') params.set('type', type);

    try {
        const data = await apiFetch(`/transactions?${params}`);
        renderTable(data.transactions);
        renderPagination(data.pagination);
    } catch (err) {
        showToast('Failed to load transactions', 'error');
    }
}

// ─── Render Table ────────────────────────────────
function renderTable(transactions) {
    const tbody = document.getElementById('transactions-body');

    if (transactions.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8">
                    <div class="empty-state">
                        <span class="material-icons-round empty-state-icon">receipt_long</span>
                        <h3>No transactions found</h3>
                        <p>Try adjusting your date range or filters</p>
                    </div>
                </td>
            </tr>
        `;
        return;
    }

    tbody.innerHTML = transactions.map((tx, i) => {
        const isIn = tx.type === 'stock_in';
        return `
            <tr style="animation: fadeSlideUp 0.3s ease ${i * 0.02}s both;">
                <td>
                    <div style="font-size: 0.85rem;">${formatDateTime(tx.created_at)}</div>
                </td>
                <td>
                    <div class="cell-item">
                        <span class="cell-icon">${getCategoryIcon(tx.category_icon)}</span>
                        <span class="cell-name">${escapeHtml(tx.item_name)}</span>
                    </div>
                </td>
                <td><span class="badge badge-category">${escapeHtml(tx.category_name)}</span></td>
                <td>
                    <span class="badge badge-${isIn ? 'stock-in' : 'stock-out'}">
                        <span class="material-icons-round" style="font-size:14px;">${isIn ? 'add_circle' : 'remove_circle'}</span>
                        ${isIn ? 'Stock In' : 'Stock Out'}
                    </span>
                </td>
                <td>
                    <strong class="${isIn ? 'text-success' : 'text-danger'}">
                        ${isIn ? '+' : '−'}${formatNumber(tx.quantity)} ${tx.item_unit}
                    </strong>
                </td>
                <td class="text-muted">
                    ${formatNumber(tx.previous_quantity)} → ${formatNumber(tx.new_quantity)}
                </td>
                <td class="text-muted" style="max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;"
                    title="${escapeHtml(tx.notes)}">
                    ${escapeHtml(tx.notes) || '—'}
                </td>
                <td class="text-muted">${escapeHtml(tx.performed_by_name)}</td>
            </tr>
        `;
    }).join('');
}

// ─── Render Pagination ───────────────────────────
function renderPagination(pagination) {
    const info = document.getElementById('table-info');
    const paginationDiv = document.getElementById('pagination');

    const start = (pagination.page - 1) * pagination.limit + 1;
    const end = Math.min(pagination.page * pagination.limit, pagination.total);
    info.textContent = pagination.total > 0
        ? `Showing ${start}–${end} of ${pagination.total} transactions`
        : 'No transactions';

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

function goToPage(page) { currentPage = page; loadTransactions(); }

// ─── Event Bindings ──────────────────────────────
function bindEvents() {
    ['filter-date-from', 'filter-date-to', 'filter-category', 'filter-type'].forEach(id => {
        document.getElementById(id)?.addEventListener('change', () => {
            currentPage = 1;
            loadTransactions();
        });
    });

    document.getElementById('btn-export-transactions')?.addEventListener('click', async () => {
        const params = new URLSearchParams();
        const dateFrom = document.getElementById('filter-date-from').value;
        const dateTo = document.getElementById('filter-date-to').value;
        const category = document.getElementById('filter-category').value;
        const type = document.getElementById('filter-type').value;

        if (dateFrom) params.set('date_from', dateFrom);
        if (dateTo) params.set('date_to', dateTo);
        if (category !== 'all') params.set('category', category);
        if (type !== 'all') params.set('type', type);

        try {
            let url = `/transactions/export/csv`;
            if (params.toString()) url += `?${params.toString()}`;
            
            const token = localStorage.getItem('cm_token');
            const res = await fetch(`${API_BASE}${url}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!res.ok) throw new Error('Export failed');
            
            const blob = await res.blob();
            const downloadUrl = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = downloadUrl;
            
            let filename = 'transactions_export.csv';
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
            
            showToast('Transactions exported successfully');
        } catch (err) {
            showToast('Failed to export', 'error');
        }
    });
}

// ─── Helpers ─────────────────────────────────────
function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str || '';
    return div.innerHTML;
}
