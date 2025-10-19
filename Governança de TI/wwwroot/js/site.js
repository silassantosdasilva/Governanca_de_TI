// Aguarda o DOM ser completamente carregado para executar todo o script
document.addEventListener('DOMContentLoaded', () => {

    // --- LÓGICA DO MENU EXPANSÍVEL DA SIDEBAR ---
    const pageContainer = document.querySelector('.page-container');
    const navItems = document.querySelectorAll('.sidebar-nav-item');
    navItems.forEach(item => {
        const link = item.querySelector('.sidebar-nav-link');
        link.addEventListener('click', (e) => {
            // Previne o comportamento padrão do link para o item de submenu
            if (link.classList.contains('has-submenu')) {
                e.preventDefault();
            }

            const isSubmenuItem = link.classList.contains('has-submenu');
            const isAlreadyActive = item.classList.contains('active');

            navItems.forEach(i => i.classList.remove('active'));

            if (isSubmenuItem) {
                if (isAlreadyActive) {
                    pageContainer.classList.remove('submenu-active');
                } else {
                    item.classList.add('active');
                    pageContainer.classList.add('submenu-active');
                }
            } else {
                item.classList.add('active');
                pageContainer.classList.remove('submenu-active');
                // Navega para o link se não for a página atual
                if (window.location.pathname !== link.getAttribute('href')) {
                    window.location.href = link.href;
                }
            }
        });
    });

    // --- LÓGICA DO DASHBOARD ---
    // Verifica se estamos na página do dashboard antes de executar o código
    if (document.getElementById('metric-cards-container')) {
        carregarDashboard();
    }

    // Função principal que busca os dados da API
    async function carregarDashboard() {
        try {
            const response = await fetch('/api/dashboard');
            if (!response.ok) {
                throw new Error(`Falha ao buscar dados: ${response.statusText}`);
            }
            const data = await response.json();

            // Chama as funções para renderizar cada parte do dashboard
            renderMetricCards(data);
            renderListaFimVidaUtil(data.equipamentosProximosFimVida);
            renderListaProximaManutencao(data.equipamentosProximaManutencao);
            criarGraficoConsumoMes(data.consumoKwhMes);
            criarGraficoConsumoAno(data.consumoKwhAno);

        } catch (error) {
            console.error("Erro ao carregar o dashboard:", error);
            // Poderia exibir uma mensagem de erro na tela para o utilizador aqui
        }
    }

    // --- FUNÇÕES DE RENDERIZAÇÃO DO DASHBOARD ---

    function renderMetricCards(data) {
        const container = document.getElementById('metric-cards-container');
        if (!container) return;
        const metrics = [
            { icon: 'bi-tree', title: 'Emissões de CO₂ Evitadas', value: data.emissoesCo2Evitadas },
            { icon: 'bi-recycle', title: 'Equipamentos Reaproveitados', value: data.equipamentosRecicladosPercentual },
            { icon: 'bi-box-seam', title: 'Itens Pendentes', value: data.itensPendentesDescarte },
            { icon: 'bi-check2-square', title: 'Descartes Corretos', value: data.equipamentosDescartadosCorretamente }
        ];
        container.innerHTML = metrics.map(m => `
            <div class="col-lg-3 col-md-6 mb-4">
                <div class="metric-card h-100 p-3">
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="title text-muted small">${m.title}</span>
                        <i class="icon ${m.icon} text-muted"></i>
                    </div>
                    <h2 class="value mt-2 fw-light">${m.value}</h2>
                </div>
            </div>`).join('');
    }

    function renderListaFimVidaUtil(equipamentos) {
        const container = document.getElementById('listaFimVidaUtil');
        if (!container) return;
        if (!equipamentos || equipamentos.length === 0) {
            container.innerHTML = '<p class="text-muted text-center mt-4">Nenhum equipamento a vencer nos próximos 5 meses.</p>';
            return;
        }
        let tableHtml = `<div class="table-responsive"><table class="table table-sm table-hover"><thead><tr><th>ID</th><th>Descrição</th><th>Vencimento</th><th class="text-end">Dias Restantes</th></tr></thead><tbody>`;
        equipamentos.forEach(item => {
            tableHtml += `<tr><td>${item.codigoItem}</td><td>${item.descricao}</td><td>${item.dataVencimento}</td><td class="text-end">${item.diasRestantes}</td></tr>`;
        });
        tableHtml += `</tbody></table></div>`;
        container.innerHTML = tableHtml;
    }

    function renderListaProximaManutencao(equipamentos) {
        const container = document.getElementById('listaProximaManutencao');
        if (!container) return;
        if (!equipamentos || equipamentos.length === 0) {
            container.innerHTML = '<p class="text-muted text-center mt-4">Nenhuma manutenção agendada para os próximos 30 dias.</p>';
            return;
        }
        let tableHtml = `<div class="table-responsive"><table class="table table-sm table-hover"><thead><tr><th>ID</th><th>Descrição</th><th>Próx. Manutenção</th><th>Frequência</th></tr></thead><tbody>`;
        equipamentos.forEach(item => {
            tableHtml += `<tr><td>${item.codigoItem}</td><td>${item.descricao}</td><td>${item.proximaManutencao}</td><td>${item.frequencia}</td></tr>`;
        });
        tableHtml += `</tbody></table></div>`;
        container.innerHTML = tableHtml;
    }

    function criarGraficoConsumoMes(chartData) {
        const ctx = document.getElementById('graficoConsumoMes');
        if (ctx && chartData) new Chart(ctx, { type: 'bar', data: { labels: chartData.labels, datasets: [{ data: chartData.data, backgroundColor: '#c4b5fd', borderRadius: 6 }] }, options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } } });
    }

    function criarGraficoConsumoAno(chartData) {
        const ctx = document.getElementById('graficoConsumoAno');
        if (ctx && chartData) new Chart(ctx, { type: 'bar', data: { labels: chartData.labels, datasets: [{ data: chartData.data, backgroundColor: '#c4b5fd', borderRadius: 6 }] }, options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } } });
    }
});

