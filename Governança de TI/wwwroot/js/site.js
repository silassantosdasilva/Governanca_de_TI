document.addEventListener('DOMContentLoaded', () => {

    // --- FUNÇÕES DE RENDERIZAÇÃO DO DASHBOARD ---

    // 1. Indicadores Superiores (Cards)
    function renderMetricCards() {
        const container = document.getElementById('metric-cards-container');
        if (!container) return;

        const metrics = [
            { icon: 'bi-tree', title: 'Emissões de CO₂ Evitadas', value: '132 kg CO₂' },
            { icon: 'bi-recycle', title: 'Equipamentos Reaproveitados ou Reciclados', value: '36.2%' },
            { icon: 'bi-box-seam', title: 'Itens Pendentes de Descarte', value: '36' },
            { icon: 'bi-check2-square', title: 'Equipamentos Descartados Corretamente', value: '29' }
        ];

        let html = '';
        metrics.forEach(metric => {
            html += `
                    <div class="col-xl-3 col-md-6">
                        <div class="metric-card">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <span class="title">${metric.title}</span>
                                <i class="icon ${metric.icon}"></i>
                            </div>
                            <h3 class="value">${metric.value}</h3>
                        </div>
                    </div>
                `;
        });
        container.innerHTML = html;
    }

    // 2. Gráfico: Economia Gerada (R$) – Mês
    function criarGraficoEconomiaMes() {
        const ctx = document.getElementById('graficoEconomiaMes');
        if (ctx) new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'],
                datasets: [{ data: [36.2, 63.8, 36.2, 63.8, 36.2, 63.8, 36.2, 63.8, 36.2, 63.8, 36.2, 63.8], backgroundColor: ['#8A2BE2', '#E6E6FA'] }]
            },
            options: { responsive: true, plugins: { legend: { position: 'right' } } }
        });
    }

    // 3. Gráfico: Consumo Total (kWh) no Mês
    function criarGraficoConsumoMes() {
        const ctx = document.getElementById('graficoConsumoMes');
        if (ctx) new Chart(ctx, {
            type: 'bar',
            data: {
                labels: ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'],
                datasets: [{ data: [7, 12, 3, 6, 10, 11, 7, 13, 19, 26, 8, 14], backgroundColor: '#8A2BE2' }]
            },
            options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } }
        });
    }

    // 4. Gráfico: Consumo Total (kWh) no Ano
    function criarGraficoConsumoAno() {
        const ctx = document.getElementById('graficoConsumoAno');
        if (ctx) new Chart(ctx, {
            type: 'bar',
            data: {
                labels: ['2012', '2013', '2014', '2015', '2016', '2017', '2018', '2019', '2020', '2021', '2022', '2023'],
                datasets: [{ data: [5000, 15000, 9500, 15500, 12500, 7500, 19000, 17500, 24500, 30500, 14500, 10000], backgroundColor: '#8A2BE2' }]
            },
            options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } }
        });
    }

    // 5. Gráfico: Consumo por Setor
    function criarGraficoConsumoSetor() {
        const ctx = document.getElementById('graficoConsumoSetor');
        if (ctx) new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Q1', 'Q2', 'Q3', 'Q4'],
                datasets: [{ data: [13.1, 28.6, 28, 30.3], backgroundColor: ['#DDA0DD', '#9370DB', '#8A2BE2', '#4B0082'] }]
            },
            options: { responsive: true, plugins: { legend: { position: 'right' } } }
        });
    }

    // --- INICIALIZAÇÃO DO DASHBOARD ---
    renderMetricCards();
    criarGraficoEconomiaMes();
    criarGraficoConsumoMes();
    criarGraficoConsumoAno();
    criarGraficoConsumoSetor();
});