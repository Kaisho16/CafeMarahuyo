// pos.js

let products = [];
let sizeModifiers = [];
let addons = [];
let promos = [];
let posSettings = { taxRate: 0, receiptFooter: "" };
let cart = [];
let currentCategory = 'All';

let currentOrderType = 'Dine-in';
let currentPaymentMode = 'Cash';
let currentDiscountType = '';
let currentDiscountValue = 0;
let currentPromoCode = null;
let amountTendered = 0;

// Customization State
let customizingProduct = null;
let customState = {
    size: null,
    temperature: 'Iced',
    iceLevel: 'Regular Ice',
    sugarLevel: 'Regular',
    selectedAddOns: [] // Array of addon objects
};

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
    if (roleEl) roleEl.textContent = user.role.toUpperCase();

    if (isAdmin) {
        const menuMgt = document.getElementById('nav-menu-management');
        if (menuMgt) menuMgt.style.display = 'flex';
        
        const invNav = document.getElementById('nav-inventory');
        if (invNav) invNav.style.display = 'flex';
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
// DATA LOADING
// ==========================================

async function loadInitialData() {
    try {
        const [prodRes, sizeRes, addRes, setRes, promoRes] = await Promise.all([
            fetchWithAuth('/api/orders/products'),
            fetchWithAuth('/api/orders/size-modifiers'),
            fetchWithAuth('/api/orders/addons'),
            fetchWithAuth('/api/orders/settings'),
            fetchWithAuth('/api/promos')
        ]);

        if (prodRes.ok) products = await prodRes.json();
        if (sizeRes.ok) sizeModifiers = await sizeRes.json();
        if (addRes.ok) addons = await addRes.json();
        if (setRes.ok) posSettings = await setRes.json();
        if (promoRes.ok) promos = await promoRes.json();

        document.getElementById('tax-rate-label').textContent = posSettings.taxRate;
        renderCategories();
        renderProducts();
        renderDiscountDropdown();
    } catch (e) {
        console.error(e);
        showToast("Error loading POS data: " + e.message, true);
    }
}

function renderCategories() {
    const container = document.getElementById('category-filters');
    if (!container) return;

    // Fixed categories requested by user
    const cats = ['All', 'Coffee', 'Frappe', 'Fruit Tea'];

    container.innerHTML = cats.map(c => 
        `<div class="category-chip ${c === currentCategory ? 'active' : ''}" onclick="setCategory('${c}')">${c}</div>`
    ).join('');
}

function setCategory(cat) {
    currentCategory = cat;
    renderCategories();
    renderProducts();
}

function renderProducts() {
    const grid = document.getElementById('product-grid');
    if (!grid) return;

    const filtered = currentCategory === 'All' 
        ? products 
        : products.filter(p => p.categoryName.toLowerCase().includes(currentCategory.toLowerCase()));
        
    grid.innerHTML = filtered.map(p => `
        <div class="product-card" onclick="openCustomizationModal(${p.id})">
            <div class="product-img-wrapper">
                <img src="${p.imageUrl || '/pos/assets/default-coffee.png'}" class="product-img" onerror="this.src='/pos/assets/default-coffee.png'">
            </div>
            <div class="product-info">
                <div class="product-name">${p.name}</div>
                ${p.description ? `<div class="product-desc">${p.description}</div>` : ''}
                <div class="product-footer">
                    <span class="product-price">₱${p.price.toFixed(2)}</span>
                    <button class="add-btn"><i class="material-icons-round" style="font-size: 18px;">add</i></button>
                </div>
            </div>
        </div>
    `).join('');
}

// ==========================================
// CUSTOMIZATION MODAL
// ==========================================

function openCustomizationModal(productId) {
    customizingProduct = products.find(p => p.id === productId);
    if (!customizingProduct) return;

    // Reset state
    customState = {
        size: sizeModifiers.length > 0 ? sizeModifiers[0].sizeName : null,
        temperature: 'Iced',
        iceLevel: 'Regular Ice',
        sugarLevel: 'Regular',
        selectedAddOns: []
    };

    document.getElementById('custom-modal-title').textContent = customizingProduct.name;
    
    // Render Sizes
    const sizeContainer = document.getElementById('size-options');
    sizeContainer.innerHTML = sizeModifiers.map(s => `
        <div class="pill ${s.sizeName === customState.size ? 'active' : ''}" onclick="selectSize(this, '${s.sizeName}')">
            ${s.sizeName} <span style="font-size: 0.8rem; opacity: 0.8;">(+₱${s.priceModifier})</span>
        </div>
    `).join('');

    // Reset pills
    document.querySelectorAll('#temp-options .pill, #ice-options .pill, #sugar-options .pill').forEach(p => p.classList.remove('active'));
    document.querySelector(`#temp-options .pill:nth-child(1)`).classList.add('active'); // Iced
    document.querySelector(`#ice-options .pill:nth-child(3)`).classList.add('active'); // Regular
    document.querySelector(`#sugar-options .pill:nth-child(3)`).classList.add('active'); // Regular
    
    const iceSection = document.getElementById('ice-section');
    if (iceSection) {
        iceSection.style.display = 'block';
        iceSection.classList.remove('hidden-section');
    }

    // Render Addons
    const addonsContainer = document.getElementById('addons-container');
    const categories = [...new Set(addons.map(a => a.category))];
    
    addonsContainer.innerHTML = categories.map(cat => {
        // Common sense check: Prevent adding milk to Americano
        if (customizingProduct.name.toLowerCase().includes('americano') && cat.toLowerCase() === 'milk') {
            return '';
        }

        const catAddons = addons.filter(a => a.category === cat);
        return `
            <div class="custom-section">
                <h3 style="text-transform: capitalize;">${cat} Options</h3>
                <div class="addon-list">
                    ${catAddons.map(a => `
                        <div class="addon-item" id="addon-btn-${a.id}" onclick="toggleAddon(${a.id}, '${cat}')">
                            <span class="addon-name">${a.name}</span>
                            <span class="addon-price">+₱${a.price.toFixed(2)}</span>
                        </div>
                    `).join('')}
                </div>
            </div>
        `;
    }).join('');

    updateCustomItemTotal();

    document.getElementById('customization-modal-overlay').classList.add('open');
    document.getElementById('customization-modal').classList.add('open');
}

function closeCustomizationModal() {
    document.getElementById('customization-modal-overlay').classList.remove('open');
    document.getElementById('customization-modal').classList.remove('open');
    customizingProduct = null;
}

function selectSize(el, sizeName) {
    document.querySelectorAll('#size-options .pill').forEach(p => p.classList.remove('active'));
    el.classList.add('active');
    customState.size = sizeName;
    updateCustomItemTotal();
}
function selectTemp(el, temp) {
    document.querySelectorAll('#temp-options .pill').forEach(p => p.classList.remove('active'));
    el.classList.add('active');
    customState.temperature = temp;
    
    const iceSection = document.getElementById('ice-section');
    if (iceSection) {
        if (temp === 'Hot') {
            iceSection.classList.add('hidden-section');
            setTimeout(() => { if (customState.temperature === 'Hot') iceSection.style.display = 'none'; }, 300);
            customState.iceLevel = ''; 
        } else {
            iceSection.style.display = 'block';
            setTimeout(() => iceSection.classList.remove('hidden-section'), 10);
            if (!customState.iceLevel) customState.iceLevel = 'Regular Ice';
        }
    }
}
function selectIce(el, level) {
    document.querySelectorAll('#ice-options .pill').forEach(p => p.classList.remove('active'));
    el.classList.add('active');
    customState.iceLevel = level;
}
function selectSugar(el, level) {
    document.querySelectorAll('#sugar-options .pill').forEach(p => p.classList.remove('active'));
    el.classList.add('active');
    customState.sugarLevel = level;
}

function toggleAddon(addonId, category) {
    const addon = addons.find(a => a.id === addonId);
    if (!addon) return;

    // For milk, it's usually single selection. Let's enforce single selection per category for Milk, allow multiple for syrups/shots
    if (category === 'milk') {
        customState.selectedAddOns = customState.selectedAddOns.filter(a => a.category !== 'milk');
        customState.selectedAddOns.push(addon);
        // UI update
        document.querySelectorAll(`.addon-item`).forEach(el => {
            if (el.getAttribute('onclick').includes("'milk'")) el.classList.remove('selected');
        });
        document.getElementById(`addon-btn-${addonId}`).classList.add('selected');
    } else {
        const idx = customState.selectedAddOns.findIndex(a => a.id === addonId);
        if (idx > -1) {
            customState.selectedAddOns.splice(idx, 1);
            document.getElementById(`addon-btn-${addonId}`).classList.remove('selected');
        } else {
            customState.selectedAddOns.push(addon);
            document.getElementById(`addon-btn-${addonId}`).classList.add('selected');
        }
    }
    updateCustomItemTotal();
}

function updateCustomItemTotal() {
    let total = customizingProduct.price;
    if (customState.size) {
        const sizeMod = sizeModifiers.find(s => s.sizeName === customState.size);
        if (sizeMod) total += sizeMod.priceModifier;
    }
    customState.selectedAddOns.forEach(a => total += a.price);
    document.getElementById('custom-item-total').textContent = `₱${total.toFixed(2)}`;
}

function addCustomizedItemToCart() {
    const item = {
        cartId: Date.now().toString(),
        product: customizingProduct,
        quantity: 1,
        customizations: { ...customState }
    };
    cart.push(item);
    closeCustomizationModal();
    updateCart();
}

// ==========================================
// CART & CHECKOUT
// ==========================================

function setOrderType(type) {
    currentOrderType = type;
    document.querySelectorAll('.order-type-btn').forEach(b => b.classList.remove('active'));
    event.target.classList.add('active');
}

function setPaymentMode(mode) {
    currentPaymentMode = mode;
    document.querySelectorAll('.payment-btn').forEach(b => b.classList.remove('active'));
    event.target.classList.add('active');
    
    const cashSec = document.getElementById('cash-tendering-section');
    const ewalletSec = document.getElementById('ewallet-reference-section');
    
    if (mode === 'Cash') {
        cashSec.style.display = 'block';
        if (ewalletSec) ewalletSec.style.display = 'none';
    } else {
        cashSec.style.display = 'none';
        if (ewalletSec) ewalletSec.style.display = 'block';
        amountTendered = 0;
        document.getElementById('amount-tendered').value = '';
    }
    updateCart();
}

function updateCartQty(cartId, delta) {
    const item = cart.find(i => i.cartId === cartId);
    if (!item) return;
    item.quantity += delta;
    if (item.quantity <= 0) {
        cart = cart.filter(i => i.cartId !== cartId);
    }
    updateCart();
}

function renderDiscountDropdown() {
    const select = document.getElementById('discount-type');
    if (!select) return;

    let html = `<option value="">No Discount</option>`;
    
    // Group active promos by category
    const activePromos = promos.filter(p => p.isActive && (!p.validUntil || new Date(p.validUntil) >= new Date()) && (!p.validFrom || new Date(p.validFrom) <= new Date()));
    
    const categories = [...new Set(activePromos.map(p => p.category || 'Promo Code'))];
    
    categories.forEach(cat => {
        html += `<optgroup label="${cat}">`;
        const catPromos = activePromos.filter(p => (p.category || 'Promo Code') === cat);
        catPromos.forEach(p => {
            const valStr = p.discountType === 'percentage' ? `${p.value}%` : `₱${p.value}`;
            html += `<option value="${p.id}">${p.code} (${valStr})</option>`;
        });
        html += `</optgroup>`;
    });

    select.innerHTML = html;
}

function applyDiscount() {
    const select = document.getElementById('discount-type');
    if (!select) return;

    const selectedId = select.value;
    if (selectedId === '') {
        currentPromoCode = null;
        currentDiscountType = '';
        currentDiscountValue = 0;
        showToast("Discount removed");
    } else {
        const p = promos.find(x => x.id.toString() === selectedId);
        if (p) {
            currentPromoCode = p.code;
            currentDiscountType = p.discountType;
            currentDiscountValue = p.value;
            showToast(`${p.category} applied!`);
        }
    }
    updateCart();
}

function addQuickCash(amt) {
    amountTendered += amt;
    document.getElementById('amount-tendered').value = amountTendered;
    calculateChange();
}

function calculateChange() {
    const input = document.getElementById('amount-tendered').value;
    amountTendered = parseFloat(input) || 0;
    
    const grandTotal = parseFloat(document.getElementById('grand-total').textContent.replace('₱','')) || 0;
    const change = Math.max(0, amountTendered - grandTotal);
    document.getElementById('change-amount').textContent = `₱${change.toFixed(2)}`;
}

function updateCart() {
    const container = document.getElementById('cart-items');
    if (cart.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <i class="material-icons-round" style="font-size: 48px; margin-bottom: 16px; opacity: 0.5;">shopping_cart</i>
                <p>No items in cart</p>
                <p style="font-size: 0.85rem; margin-top: 4px;">Select items from the menu to add them</p>
            </div>
        `;
        document.getElementById('checkout-btn').disabled = true;
        document.getElementById('subtotal').textContent = '₱0.00';
        document.getElementById('tax-amount').textContent = '₱0.00';
        document.getElementById('grand-total').textContent = '₱0.00';
        document.getElementById('discount-row').style.display = 'none';
        document.getElementById('change-row').style.display = 'none';
        return;
    }

    document.getElementById('checkout-btn').disabled = false;
    
    let subtotal = 0;
    container.innerHTML = cart.map(item => {
        let unitPrice = item.product.price;
        if (item.customizations.size) {
            const sMod = sizeModifiers.find(s => s.sizeName === item.customizations.size);
            if (sMod) unitPrice += sMod.priceModifier;
        }
        item.customizations.selectedAddOns.forEach(a => unitPrice += a.price);
        
        const itemTotal = unitPrice * item.quantity;
        subtotal += itemTotal;

        const addonsList = item.customizations.selectedAddOns.map(a => `+${a.name}`).join(', ');
        const customText = [
            item.customizations.size,
            item.customizations.temperature,
            item.customizations.sugarLevel,
            item.customizations.iceLevel,
            addonsList
        ].filter(Boolean).join(' • ');

        return `
            <div class="cart-item">
                <div class="cart-item-info">
                    <div class="cart-item-name">${item.product.name}</div>
                    <div class="cart-item-customizations">${customText}</div>
                    <div class="cart-item-price">₱${unitPrice.toFixed(2)}</div>
                </div>
                <div class="qty-control">
                    <button class="qty-btn" onclick="updateCartQty('${item.cartId}', 1)"><i class="material-icons-round">add</i></button>
                    <div class="qty-val">${item.quantity}</div>
                    <button class="qty-btn" onclick="updateCartQty('${item.cartId}', -1)"><i class="material-icons-round">remove</i></button>
                </div>
            </div>
        `;
    }).join('');

    document.getElementById('subtotal').textContent = `₱${subtotal.toFixed(2)}`;

    let discount = 0;
    if (currentDiscountType === 'percentage') {
        discount = subtotal * (currentDiscountValue / 100);
    } else if (currentDiscountType === 'fixed') {
        discount = currentDiscountValue;
    }
    if (discount > subtotal) discount = subtotal;

    const discountRow = document.getElementById('discount-row');
    if (discount > 0) {
        discountRow.style.display = 'flex';
        document.getElementById('discount-amount').textContent = `-₱${discount.toFixed(2)}`;
    } else {
        discountRow.style.display = 'none';
    }

    const discountedSub = subtotal - discount;
    const grand = discountedSub;
    const tax = discountedSub * (posSettings.taxRate / 100);

    document.getElementById('tax-amount').textContent = `₱${tax.toFixed(2)}`;
    document.getElementById('grand-total').textContent = `₱${grand.toFixed(2)}`;

    if (currentPaymentMode === 'Cash') {
        document.getElementById('change-row').style.display = 'flex';
        calculateChange();
    } else {
        document.getElementById('change-row').style.display = 'none';
    }
}

async function checkout() {
    const btn = document.getElementById('checkout-btn');
    if (btn && btn.disabled) return;
    if (btn) {
        btn.disabled = true;
        btn.textContent = "Processing...";
    }

    const isEWallet = currentPaymentMode === 'E-Wallet';
    const isManualReference = document.getElementById('reference-code') && document.getElementById('reference-code').value.trim() !== '';

    if (isManualReference) {
        const refValue = document.getElementById('reference-code').value.trim();
        const refRegex = /^\d{9,16}$/;
        if (!refRegex.test(refValue)) {
            showToast('Reference code must be between 9 and 16 digits.', 'error');
            if (btn) {
                btn.disabled = false;
                btn.textContent = "Checkout";
            }
            return;
        }
    }

    const req = {
        orderType: currentOrderType,
        paymentMode: currentPaymentMode,
        amountTendered: currentPaymentMode === 'Cash' ? amountTendered : 0,
        discountType: currentDiscountType,
        discountValue: currentDiscountValue,
        promoCode: currentPromoCode,
        referenceCode: isManualReference ? document.getElementById('reference-code').value : null,
        useXendit: isEWallet && !isManualReference, // Only use Xendit if E-Wallet is selected AND no manual reference code is provided
        items: cart.map(i => ({
            productId: i.product.id,
            quantity: i.quantity,
            size: i.customizations.size,
            temperature: i.customizations.temperature,
            iceLevel: i.customizations.iceLevel,
            sugarLevel: i.customizations.sugarLevel,
            addOns: i.customizations.selectedAddOns.map(a => ({ addOnId: a.id, name: a.name, price: a.price }))
        }))
    };

    try {
        const res = await fetchWithAuth('/api/orders/checkout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(req)
        });
        
        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.error || "Checkout failed");
        }
        
        const data = await res.json();
        
        if (data.paymentUrl) {
            showToast("Opening Xendit Payment Portal...", false);
            window.open(data.paymentUrl, '_blank');
        } else {
            showToast("Order Success!");
        }
        
        // Fetch and show receipt
        fetchAndShowReceipt(data.orderId);

        // Reset cart
        cart = [];
        currentDiscountType = '';
        currentDiscountValue = 0;
        currentPromoCode = null;
        amountTendered = 0;
        document.getElementById('discount-type').value = '';
        document.getElementById('discount-value').style.display = 'none';
        document.getElementById('discount-value').value = '';
        document.getElementById('amount-tendered').value = '';
        if (document.getElementById('reference-code')) {
            document.getElementById('reference-code').value = '';
        }
        updateCart();
    } catch (e) {
        showToast(e.message, true);
    } finally {
        btn.disabled = false;
        btn.textContent = "Checkout & Print";
    }
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
if (window.location.pathname.includes('index.html') || window.location.pathname.endsWith('pos/')) {
    document.addEventListener('DOMContentLoaded', () => {
        initAuth();
        loadInitialData();
        updateCart();
    });
}
