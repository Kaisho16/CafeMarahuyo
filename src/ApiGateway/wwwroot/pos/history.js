// history.js

function initAuth() {
    const token = localStorage.getItem('cm_token');
    const userJson = localStorage.getItem('cm_user');
    
    if (!token || !userJson) {
        window.location.href = '/index.html';
        return;
    }
    
    const user = JSON.parse(userJson);
    const cashierEl = document.getElementById('cashier-name');
    if (cashierEl) cashierEl.textContent = user.displayName;

    const isAdmin = ['admin', 'Inventory Manager', 'POS Manager'].includes(user.role);
    const roleEl = document.getElementById('cashier-role');
    if (roleEl) roleEl.textContent = isAdmin ? 'POS Manager' : 'Cashier';

    if (isAdmin) {
        const menuMgt = document.getElementById('nav-menu-management');
        if (menuMgt) menuMgt.style.display = 'flex';
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

async function fetchWithAuth(url, options = {}) {
    const token = localStorage.getItem('cm_token');
    options.headers = {
        ...options.headers,
        'Authorization': `Bearer ${token}`
    };
    const res = await fetch(url, options);
    if (res.status === 401) { logout(); throw new Error("Unauthorized"); }
    return res;
}

// ==========================================
// DATA FETCHING
// ==========================================

async function fetchHistory() {
    try {
        // Summary
        const sumRes = await fetchWithAuth('/api/orders/history/summary?period=daily');
        if (sumRes.ok) {
            const sumData = await sumRes.json();
            document.getElementById('total-orders').textContent = sumData.totalOrders;
            document.getElementById('total-revenue').textContent = `₱${sumData.totalRevenue.toFixed(2)}`;
        }

        // Filters
        const search = document.getElementById('search-input').value;
        const dateFrom = document.getElementById('date-from').value;
        const dateTo = document.getElementById('date-to').value;
        const pMode = document.getElementById('payment-mode-filter').value;
        const oType = document.getElementById('order-type-filter').value;

        const params = new URLSearchParams();
        if (search) params.append('search', search);
        if (dateFrom) params.append('dateFrom', dateFrom);
        if (dateTo) params.append('dateTo', dateTo);
        if (pMode) params.append('paymentMode', pMode);
        if (oType) params.append('orderType', oType);

        const res = await fetchWithAuth(`/api/orders/history?${params.toString()}`);
        if (!res.ok) throw new Error("Failed to load history");

        const orders = await res.json();
        renderHistoryTable(orders);

    } catch (e) {
        console.error(e);
        showToast("Error loading history data", true);
    }
}

function renderHistoryTable(orders) {
    const tbody = document.getElementById('history-tbody');
    
    if (orders.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="empty-state">No orders found matching criteria.</td></tr>`;
        return;
    }

    tbody.innerHTML = orders.map(o => {
        const pBadge = o.paymentMode === 'Cash' ? 'badge-cash' : 'badge-digital';
        const itemNames = o.items.map(i => `${i.quantity}x ${i.productName}`).join(', ');
        const truncItems = itemNames.length > 40 ? itemNames.substring(0, 37) + '...' : itemNames;

        return `
            <tr onclick="fetchAndShowReceipt(${o.id})">
                <td style="font-weight: 600;">${o.orderNumber}</td>
                <td style="color: var(--text-secondary);">${o.orderDate}</td>
                <td><span class="badge badge-type">${o.orderType}</span></td>
                <td style="font-size: 0.9rem;" title="${itemNames}">${truncItems}</td>
                <td>${o.cashierName}</td>
                <td style="font-weight: 700;">₱${o.totalAmount.toFixed(2)}</td>
                <td><span class="badge ${pBadge}">${o.paymentMode}</span></td>
            </tr>
        `;
    }).join('');
}


// ==========================================
// RECEIPT MODAL
// ==========================================

async function fetchAndShowReceipt(orderId) {
    try {
        const res = await fetchWithAuth(`/api/orders/${orderId}/receipt`);
        if (!res.ok) throw new Error("Failed to load receipt");
        
        const order = await res.json();
        
        const rHtml = `
            <div class="receipt-header">
                <div class="receipt-logo">MARAHUYO</div>
                <div class="receipt-info">Flavors of Comfort</div>
                <div class="receipt-info">Order: ${order.orderNumber}</div>
                <div class="receipt-info">${order.orderDate}</div>
                <div class="receipt-info">Type: ${order.orderType}</div>
                <div class="receipt-info">Cashier: ${order.cashierName}</div>
            </div>
            
            <div class="receipt-items">
                ${order.items.map(i => {
                    const mods = [];
                    if (i.size) mods.push(i.size);
                    if (i.temperature) mods.push(i.temperature);
                    if (i.sugarLevel) mods.push(i.sugarLevel);
                    if (i.iceLevel) mods.push(i.iceLevel);
                    
                    let addonText = '';
                    if (i.customizationsJson) {
                        try {
                            const addObjs = JSON.parse(i.customizationsJson);
                            addObjs.forEach(a => mods.push(a.name));
                        } catch(e){}
                    }
                    
                    return `
                    <div style="margin-bottom: 8px;">
                        <div class="receipt-item-row">
                            <span>${i.quantity}x ${i.productName}</span>
                            <span>${i.subtotal.toFixed(2)}</span>
                        </div>
                        ${mods.length > 0 ? `<div class="receipt-item-mods">${mods.join(', ')}</div>` : ''}
                    </div>
                    `;
                }).join('')}
            </div>
            
            <div class="receipt-totals">
                <div class="receipt-total-row">
                    <span>Subtotal</span>
                    <span>${order.subtotal.toFixed(2)}</span>
                </div>
                ${order.promoDiscount > 0 ? `
                <div class="receipt-total-row">
                    <span>Discount</span>
                    <span>-${order.promoDiscount.toFixed(2)}</span>
                </div>` : ''}
                <div class="receipt-total-row">
                    <span>Tax</span>
                    <span>${order.taxAmount.toFixed(2)}</span>
                </div>
                <div class="receipt-total-row receipt-grand-total">
                    <span>TOTAL</span>
                    <span>${order.totalAmount.toFixed(2)}</span>
                </div>
            </div>
            
            <div class="receipt-totals">
                <div class="receipt-total-row">
                    <span>Payment (${order.paymentMode})</span>
                    <span>${order.amountTendered.toFixed(2)}</span>
                </div>
                <div class="receipt-total-row">
                    <span>Change</span>
                    <span>${order.changeAmount.toFixed(2)}</span>
                </div>
            </div>
            
            <div class="receipt-footer-msg">
                ${order.receiptFooter || 'Thank you!'}
            </div>
        `;
        
        document.getElementById('receipt-content').innerHTML = rHtml;
        document.getElementById('receipt-modal-overlay').classList.add('open');
        document.getElementById('receipt-modal').classList.add('open');

    } catch(e) {
        console.error(e);
        showToast("Error loading receipt", true);
    }
}

function closeReceiptModal() {
    document.getElementById('receipt-modal-overlay').classList.remove('open');
    document.getElementById('receipt-modal').classList.remove('open');
}

function printReceipt() {
    const content = document.getElementById('receipt-content').innerHTML;
    const win = window.open('', '_blank', 'width=400,height=600');
    win.document.write(`
        <html>
        <head>
            <style>
                body { font-family: 'Courier New', Courier, monospace; color: #000; padding: 20px; font-size: 14px;}
                .receipt-header { text-align: center; margin-bottom: 24px; border-bottom: 1px dashed #000; padding-bottom: 16px; }
                .receipt-logo { font-size: 1.5rem; font-weight: 900; margin-bottom: 4px; font-family: sans-serif;}
                .receipt-info { font-size: 0.85rem; }
                .receipt-items { margin-bottom: 24px; border-bottom: 1px dashed #000; padding-bottom: 16px; }
                .receipt-item-row { display: flex; justify-content: space-between; font-size: 0.9rem; margin-bottom: 4px; }
                .receipt-item-mods { font-size: 0.8rem; margin-left: 12px; margin-bottom: 8px; }
                .receipt-totals { margin-bottom: 24px; border-bottom: 1px dashed #000; padding-bottom: 16px; }
                .receipt-total-row { display: flex; justify-content: space-between; font-size: 0.9rem; margin-bottom: 4px; }
                .receipt-grand-total { font-size: 1.2rem; font-weight: 800; margin-top: 8px; }
                .receipt-footer-msg { text-align: center; font-size: 0.85rem; }
            </style>
        </head>
        <body onload="window.print(); window.close();">
            ${content}
        </body>
        </html>
    `);
    win.document.close();
}


// INIT
if (window.location.pathname.includes('history.html')) {
    document.addEventListener('DOMContentLoaded', () => {
        initAuth();
        fetchHistory();
        
        // Setup enter key for search
        document.getElementById('search-input').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') fetchHistory();
        });
    });
}
