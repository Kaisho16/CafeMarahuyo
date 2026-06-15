/**
 * Cafe Marahuyo — Dashboard Page Script (v2)
 * 
 * Loads summary stats, charts, and low-stock alerts.
 * v2: Material Icons, theme-aware charts, Poppins font.
 */

document.addEventListener('DOMContentLoaded', () => {
    if (!requireAuthentication()) return;

    const dateEl = document.getElementById('current-date');
    if (dateEl) {
        dateEl.textContent = new Date().toLocaleDateString('en-PH', {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    }

    loadDashboard();
});

async function loadDashboard() {
    try {
        const [summary, lowStock, usageChart, categoryBreakdown] = await Promise.all([
            apiFetch('/dashboard/summary'),
            apiFetch('/dashboard/low-stock'),
            apiFetch('/dashboard/usage-chart'),
            apiFetch('/dashboard/category-breakdown')
        ]);

        // Update stat cards
        updateStats(summary);

        // Build charts
        buildUsageChart(usageChart);
        buildCategoryChart(categoryBreakdown);

        // Display low stock alerts
        displayLowStock(lowStock);

    } catch (err) {
        console.error('Dashboard load error:', err);
        showToast('Failed to load dashboard data', 'error');
    }
}

function updateStats(summary) {
    document.getElementById('total-items').textContent = summary.totalItems;
    document.getElementById('low-stock-count').textContent = summary.lowStockCount;
    document.getElementById('total-value').textContent = formatCurrency(summary.totalValue);
    document.getElementById('today-activity').textContent = summary.todayTransactions;
    
    const todayDetail = document.getElementById('today-detail');
    if (todayDetail) {
        todayDetail.textContent = `In: ${formatNumber(summary.todayStockIn)} · Out: ${formatNumber(summary.todayStockOut)}`;
    }

    // Mark low stock card as danger if there are alerts
    const lowStockCard = document.getElementById('stat-low');
    if (lowStockCard && summary.lowStockCount > 0) {
        lowStockCard.classList.add('danger');
    }

    // Animate stat values
    document.querySelectorAll('.stat-card').forEach((card, i) => {
        card.style.animation = `fadeSlideUp 0.4s ease ${i * 0.08}s both`;
    });
}

function getChartThemeColors() {
    const isDark = getTheme() === 'dark';
    return {
        textColor: isDark ? '#a89585' : '#6b5c4f',
        gridColor: isDark ? 'rgba(200, 149, 108, 0.06)' : 'rgba(160, 107, 62, 0.08)',
        tooltipBg: isDark ? '#241d17' : '#ffffff',
        tooltipTitle: isDark ? '#f5efe8' : '#2c1f14',
        tooltipBody: isDark ? '#a89585' : '#6b5c4f',
        tooltipBorder: isDark ? 'rgba(200, 149, 108, 0.2)' : 'rgba(160, 107, 62, 0.15)',
        doughnutBorder: isDark ? '#241d17' : '#ffffff'
    };
}

function buildUsageChart(data) {
    const ctx = document.getElementById('usage-chart');
    if (!ctx) return;

    const t = getChartThemeColors();

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.labels,
            datasets: [
                {
                    label: 'Stock In',
                    data: data.stockInData,
                    backgroundColor: 'rgba(92, 184, 92, 0.7)',
                    borderColor: 'rgba(92, 184, 92, 1)',
                    borderWidth: 1,
                    borderRadius: 6,
                    barPercentage: 0.7,
                    categoryPercentage: 0.6
                },
                {
                    label: 'Stock Out',
                    data: data.stockOutData,
                    backgroundColor: 'rgba(224, 85, 85, 0.7)',
                    borderColor: 'rgba(224, 85, 85, 1)',
                    borderWidth: 1,
                    borderRadius: 6,
                    barPercentage: 0.7,
                    categoryPercentage: 0.6
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        color: t.textColor,
                        padding: 16,
                        usePointStyle: true,
                        pointStyleWidth: 10,
                        font: { family: 'Poppins', size: 12 }
                    }
                },
                tooltip: {
                    backgroundColor: t.tooltipBg,
                    titleColor: t.tooltipTitle,
                    bodyColor: t.tooltipBody,
                    borderColor: t.tooltipBorder,
                    borderWidth: 1,
                    cornerRadius: 8,
                    padding: 12,
                    titleFont: { family: 'Poppins' },
                    bodyFont: { family: 'Poppins' }
                }
            },
            scales: {
                x: {
                    grid: { color: t.gridColor },
                    ticks: { color: t.textColor, font: { family: 'Poppins', size: 12 } }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: t.gridColor },
                    ticks: { color: t.textColor, font: { family: 'Poppins', size: 12 } }
                }
            }
        }
    });
}

function buildCategoryChart(data) {
    const ctx = document.getElementById('category-chart');
    if (!ctx) return;

    const t = getChartThemeColors();

    const colors = [
        '#c8956c',
        '#7fb38e',
        '#e0a855',
        '#b07070',
        '#6ba3c8'
    ];

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.labels,
            datasets: [{
                data: data.values,
                backgroundColor: colors.slice(0, data.labels.length),
                borderColor: t.doughnutBorder,
                borderWidth: 3,
                hoverBorderWidth: 0,
                hoverOffset: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '65%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: t.textColor,
                        padding: 12,
                        usePointStyle: true,
                        pointStyleWidth: 10,
                        font: { family: 'Poppins', size: 11 }
                    }
                },
                tooltip: {
                    backgroundColor: t.tooltipBg,
                    titleColor: t.tooltipTitle,
                    bodyColor: t.tooltipBody,
                    borderColor: t.tooltipBorder,
                    borderWidth: 1,
                    cornerRadius: 8,
                    padding: 12,
                    callbacks: {
                        label: function(context) {
                            return ` ${context.label}: ${formatCurrency(context.parsed)}`;
                        }
                    }
                }
            }
        }
    });
}

function displayLowStock(items) {
    const grid = document.getElementById('low-stock-grid');
    const countBadge = document.getElementById('alert-count');
    const section = document.getElementById('low-stock-section');

    if (!grid) return;

    countBadge.textContent = items.length;

    if (items.length === 0) {
        section.style.display = 'none';
        return;
    }

    section.style.display = '';

    grid.innerHTML = items.map((item, i) => `
        <div class="low-stock-item" style="animation: fadeSlideUp 0.3s ease ${i * 0.05}s both;">
            <span class="low-stock-icon">${getCategoryIcon(item.category_icon)}</span>
            <div class="low-stock-info">
                <div class="low-stock-name">${escapeHtml(item.name)}</div>
                <div class="low-stock-category">${escapeHtml(item.category_name)}</div>
            </div>
            <div class="low-stock-qty">
                <div class="low-stock-current">${formatNumber(item.quantity)} ${item.unit}</div>
                <div class="low-stock-min">min: ${formatNumber(item.min_stock_level)}</div>
            </div>
        </div>
    `).join('');
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}
