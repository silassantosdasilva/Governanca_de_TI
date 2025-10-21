// Aguarda o DOM ser completamente carregado para executar todo o script
document.addEventListener('DOMContentLoaded', () => {

    // --- Seletores Globais ---
    const pageContainer = document.querySelector('.page-container');
    const sidebar = document.querySelector('.sidebar');
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const sidebarOverlay = document.getElementById('sidebar-overlay');
    const navItemsWithSubmenu = document.querySelectorAll('.sidebar-nav-item .has-submenu');

    // ============================================================
    // LÓGICA DA SIDEBAR RESPONSIVA (MENU HAMBÚRGUER)
    // ============================================================
    if (sidebar && sidebarToggle && sidebarOverlay) {
        const closeSidebar = () => {
            sidebar.classList.remove('is-open');
            sidebarOverlay.classList.remove('is-visible');
        };

        const openSidebar = () => {
            sidebar.classList.add('is-open');
            sidebarOverlay.classList.add('is-visible');
        };

        // Abre/Fecha a sidebar ao clicar no botão hambúrguer
        sidebarToggle.addEventListener('click', (e) => {
            e.stopPropagation(); // Impede que o clique feche o menu imediatamente
            if (sidebar.classList.contains('is-open')) {
                closeSidebar();
            } else {
                openSidebar();
            }
        });

        // Fecha a sidebar ao clicar no overlay
        sidebarOverlay.addEventListener('click', closeSidebar);
    }

    // ============================================================
    // LÓGICA DO SUBMENU EXPANSÍVEL (DESKTOP E MOBILE)
    // ============================================================
    if (navItemsWithSubmenu.length > 0 && pageContainer) {
        navItemsWithSubmenu.forEach(triggerLink => {
            triggerLink.addEventListener('click', (event) => {
                event.preventDefault();
                const parentNavItem = triggerLink.closest('.sidebar-nav-item');
                const isAlreadyActive = parentNavItem.classList.contains('active');

                // Fecha todos os outros itens de menu
                document.querySelectorAll('.sidebar-nav-item').forEach(item => {
                    if (item !== parentNavItem) {
                        item.classList.remove('active');
                    }
                });

                // Abre ou fecha o item clicado
                parentNavItem.classList.toggle('active');

                // Controla a margem do conteúdo principal em DESKTOP
                if (window.innerWidth >= 992) {
                    if (isAlreadyActive) {
                        pageContainer.classList.remove('submenu-active');
                    } else {
                        pageContainer.classList.add('submenu-active');
                    }
                }
            });
        });

        // Fecha o submenu se clicar fora dele (apenas em desktop)
        document.addEventListener('click', (event) => {
            if (window.innerWidth < 992) return;

            const activeSubmenuItem = document.querySelector('.sidebar-nav-item.active');
            if (activeSubmenuItem && !activeSubmenuItem.contains(event.target)) {
                activeSubmenuItem.classList.remove('active');
                pageContainer.classList.remove('submenu-active');
            }
        });
    }

    // ============================================================
    // LÓGICA DO DASHBOARD (Mantida do seu código original)
    // ============================================================
    if (document.getElementById('metric-cards-container')) {
        carregarDashboard();
    }

    async function carregarDashboard() {
        try {
            const response = await fetch('/api/dashboard');
            if (!response.ok) {
                throw new Error(`Falha ao buscar dados: ${response.statusText}`);
            }
            const data = await response.json();

            renderMetricCards(data);
            renderListaFimVidaUtil(data.equipamentosProximosFimVida);
            renderListaProximaManutencao(data.equipamentosProximaManutencao);
            criarGraficoConsumoMes(data.consumoKwhMes);
            criarGraficoConsumoAno(data.consumoKwhAno);

        } catch (error) {
            console.error("Erro ao carregar o dashboard:", error);
        }
    }

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

