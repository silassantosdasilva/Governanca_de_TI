// ============================================================
// SITE.JS - TechGreen Neo 2025
// ============================================================

// ============================================================
// NOTIFICAÇÕES
// ============================================================

// Define a cor padrão dos gráficos (com base no tema atual)
Chart.defaults.color = getComputedStyle(document.body).getPropertyValue('--text-color');

// Evita recarregar notificações mais de uma vez
let notificationsLoaded = false;

// Elementos do DOM usados nas notificações
const $loader = $('#notification-loader');
const $gamificationContainer = $('#notification-gamification-content');
const $equipamentosContainer = $('#notification-equipamentos-content');
const $descartesContainer = $('#notification-descartes-content');

// Evento do sino de notificações (carrega apenas na primeira vez)
$('#notification-bell-icon').one('click', function () {
    if (notificationsLoaded) return;

    $loader.show();

    $.get('/api/NotificationApi/summary')
        .done(function (data) {
            // Renderiza gamificação ESG
            renderNotificationGamification(data.gamificacao);

            // Renderiza notificações de equipamentos
            renderNotificationList(
                $equipamentosContainer,
                "Vencimentos Próximos",
                "bi-calendar-x",
                "text-warning",
                data.equipamentosVencendo,
                item => `<strong>${item.descricao}</strong> vence em ${item.diasRestantes} dias.`,
                "Nenhum equipamento vencendo.",
                '/Equipamentos/Consulta'
            );

            // Renderiza notificações de descartes
            renderNotificationList(
                $descartesContainer,
                "Descartes Recentes",
                "bi-recycle",
                "text-success",
                data.descartesRecentes,
                item => `<strong>${item.empresaColetora}</strong> coletou ${item.descricaoEquipamento}.`,
                "Nenhum descarte recente.",
                '/Descarte/Consulta'
            );

            notificationsLoaded = true;
        })
        .fail(jqXHR => {
            console.error("Erro Notificações:", jqXHR.status, jqXHR.responseText);
            $gamificationContainer.html('<li><div class="dropdown-item text-danger small px-3">Erro ao carregar notificações.</div></li>');
        })
        .always(() => $loader.hide());
});

// ============================================================
// FUNÇÕES AUXILIARES DE NOTIFICAÇÕES
// ============================================================

/**
 * Renderiza a notificação de gamificação ESG dentro do menu suspenso.
 */
function renderNotificationGamification(gamificacao) {
    const container = $('#notification-gamification-content');
    container.empty();

    if (!gamificacao) {
        container.html(`
            <li>
                <div class="dropdown-item text-muted small px-3">
                    Nenhuma conquista recente.
                </div>
            </li>
        `);
        return;
    }

    container.html(`
        <li>
            <div class="notification-item">
                <div class="notification-icon text-primary">
                    <i class="bi bi-star-fill"></i>
                </div>
                <div>
                    <p class="text-body mb-1">
                        <strong>${gamificacao.titulo || 'Nova Conquista!'}</strong>
                    </p>
                    <small class="text-muted">
                        ${gamificacao.mensagem || 'Você avançou em seu progresso ESG!'}
                    </small>
                </div>
            </div>
        </li>
    `);
}

/**
 * Renderiza uma lista genérica de notificações (equipamentos, descartes, etc).
 */
function renderNotificationList(container, titulo, icon, iconColor, lista, formatItem, vazioMsg, link) {
    container.empty();

    // Cabeçalho
    container.append(`
        <li>
            <h6 class="dropdown-header fw-semibold">
                <i class="bi ${icon} ${iconColor} me-2"></i>${titulo}
            </h6>
        </li>
    `);

    // Lista de itens
    if (lista && lista.length > 0) {
        lista.forEach(item => {
            container.append(`
                <li>
                    <div class="notification-item">
                        <div class="notification-icon ${iconColor}">
                            <i class="bi ${icon}"></i>
                        </div>
                        <div>
                            <p class="text-body mb-1">${formatItem(item)}</p>
                            <small class="text-muted">${new Date().toLocaleDateString('pt-BR')}</small>
                        </div>
                    </div>
                </li>
            `);
        });

        container.append(`
            <li>
                <a href="${link}" class="dropdown-item text-center small text-primary">
                    Ver todos
                </a>
            </li>
        `);
    } else {
        container.append(`
            <li>
                <div class="dropdown-item text-muted small px-3">${vazioMsg}</div>
            </li>
        `);
    }
}

// ============================================================
// DASHBOARD PRINCIPAL
// ============================================================

if (document.getElementById('metric-cards-container')) {
    carregarDashboard();
}

// Função assíncrona para carregar todos os dados do dashboard
async function carregarDashboard() {
    try {
        const response = await fetch('/api/Dashboard');
        if (!response.ok) throw new Error(`Falha ao buscar dados: ${response.statusText}`);

        const data = await response.json();

        // Renderizações
        renderizarGamificacao(data.gamificacao);
        renderMetricCards(data);
        renderListaFimVidaUtil(data.equipamentosProximosFimVida);
        renderListaProximaManutencao(data.equipamentosProximaManutencao);

        // Gráficos
        await carregarGraficoConsumoMes();
        await carregarGraficoConsumoAno();

    } catch (error) {
        console.error("Erro ao carregar o dashboard:", error);
    }
}

// ============================================================
// GAMIFICAÇÃO ESG (Painel principal)
// ============================================================

function renderizarGamificacao(g) {
    const iconeNivel = document.getElementById('iconeNivelESG');
    const nivelTexto = document.getElementById('nivelESG');
    const barra = document.getElementById('barraProgressoESG');
    const pontos = document.getElementById('pontosESG');
    const mensagem = document.getElementById('mensagemNivel');

    if (!g) return;

    if (iconeNivel) iconeNivel.textContent = g.iconeNivel || "🌱";
    if (nivelTexto) nivelTexto.textContent = g.nivelAtual || "Iniciante";
    if (barra) barra.style.width = (g.percentualProgresso || 5) + "%";
    if (pontos) pontos.textContent = `${g.pontosAtuais || 5} / ${g.pontosProximoNivel || 100}`;
    if (mensagem) mensagem.textContent = g.mensagemNivel || "Falta pouco para o próximo nível!";
}

// ============================================================
// MÉTRICAS SUPERIORES
// ============================================================

function renderMetricCards(data) {
    const container = document.getElementById('metric-cards-container');
    if (!container) return;

    const metrics = [
        { icon: 'bi-tree', title: 'Emissões de CO₂ Evitadas', value: data.emissoesCo2Evitadas ?? '0' },
        { icon: 'bi-recycle', title: 'Equipamentos Reaproveitados', value: data.equipamentosRecicladosPercentual ?? '0%' },
        { icon: 'bi-box-seam', title: 'Itens Pendentes', value: data.itensPendentesDescarte ?? '0' },
        { icon: 'bi-check2-square', title: 'Descartes Corretos', value: data.equipamentosDescartadosCorretamente ?? '0' }
    ];

    container.innerHTML = metrics.map(m => `
        <div class="col-lg-3 col-md-6 mb-4">
            <div class="metric-card h-100 p-3 shadow-sm border-0 rounded-4">
                <div class="d-flex justify-content-between align-items-start">
                    <span class="title text-muted small">${m.title}</span>
                    <i class="icon ${m.icon} text-muted"></i>
                </div>
                <h2 class="value mt-2 fw-light">${m.value}</h2>
            </div>
        </div>
    `).join('');
}

// ============================================================
// LISTAS DE EQUIPAMENTOS
// ============================================================

function renderListaFimVidaUtil(equipamentos) {
    const container = document.getElementById('listaFimVidaUtil');
    if (!container) return;

    if (!equipamentos || equipamentos.length === 0) {
        container.innerHTML = '<p class="text-muted text-center mt-4">Nenhum equipamento a vencer nos próximos meses.</p>';
        return;
    }

    container.innerHTML = `
        <table class="table table-sm table-hover">
            <thead>
                <tr><th>ID</th><th>Descrição</th><th>Vencimento</th><th class="text-end">Dias Restantes</th></tr>
            </thead>
            <tbody>
                ${equipamentos.map(item => `
                    <tr>
                        <td>${item.codigoItem}</td>
                        <td>${item.descricao}</td>
                        <td>${item.dataVencimento}</td>
                        <td class="text-end">${item.diasRestantes}</td>
                    </tr>
                `).join('')}
            </tbody>
        </table>`;
}

function renderListaProximaManutencao(equipamentos) {
    const container = document.getElementById('listaProximaManutencao');
    if (!container) return;

    if (!equipamentos || equipamentos.length === 0) {
        container.innerHTML = '<p class="text-muted text-center mt-4">Nenhuma manutenção agendada.</p>';
        return;
    }

    container.innerHTML = `
        <table class="table table-sm table-hover">
            <thead>
                <tr><th>ID</th><th>Descrição</th><th>Próx. Manutenção</th><th>Frequência</th></tr>
            </thead>
            <tbody>
                ${equipamentos.map(item => `
                    <tr>
                        <td>${item.codigoItem}</td>
                        <td>${item.descricao}</td>
                        <td>${item.proximaManutencao}</td>
                        <td>${item.frequencia}</td>
                    </tr>
                `).join('')}
            </tbody>
        </table>`;
}

// ============================================================
// GRÁFICOS
// ============================================================

async function carregarGraficoConsumoMes() {
    try {
        const response = await fetch('/ConsumoEnergia/ObterConsumoMensal');
        if (!response.ok) throw new Error('Falha ao obter dados mensais');

        const dados = await response.json();
        const labels = dados.map(d => new Date(2025, d.mes - 1).toLocaleString('pt-BR', { month: 'short' }));
        const valores = dados.map(d => d.totalKwh);

        criarGraficoConsumoMes({ labels, data: valores });
    } catch (e) {
        console.warn('Gráfico mensal não carregado:', e.message);
    }
}

async function carregarGraficoConsumoAno() {
    try {
        const response = await fetch('/ConsumoEnergia/ObterConsumoAnual');
        if (!response.ok) throw new Error('Falha ao obter dados anuais');

        const dados = await response.json();
        const labels = dados.map(d => d.ano);
        const valores = dados.map(d => d.totalKwh);

        criarGraficoConsumoAno({ labels, data: valores });
    } catch (e) {
        console.warn('Gráfico anual não carregado:', e.message);
    }
}

// ============================================================
// CRIAÇÃO DE GRÁFICOS (Chart.js)
// ============================================================

function criarGraficoConsumoMes(chartData) {
    const ctx = document.getElementById('graficoConsumoMes');
    if (!ctx) return;

    if (window.graficoMesInstance) window.graficoMesInstance.destroy();

    window.graficoMesInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: chartData.labels,
            datasets: [{
                data: chartData.data,
                fill: true,
                backgroundColor: 'rgba(138, 43, 226, 0.15)',
                borderColor: '#8A2BE2',
                tension: 0.3
            }]
        },
        options: {
            responsive: true,
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true } }
        }
    });
}

function criarGraficoConsumoAno(chartData) {
    const ctx = document.getElementById('graficoConsumoAno');
    if (!ctx) return;

    if (window.graficoAnoInstance) window.graficoAnoInstance.destroy();

    window.graficoAnoInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: chartData.labels,
            datasets: [{
                data: chartData.data,
                backgroundColor: 'rgba(138, 43, 226, 0.25)',
                borderColor: '#8A2BE2',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true } }
        }
    });
}


// =================== MOSTRAR TOAST GAMIFICAÇÃO ===================
function mostrarToastGamificacao(msg) {
    $("#toastGamificacaoMessage").text(msg);
    $("#toastGamificacao").fadeIn(200);

    setTimeout(() => {
        $("#toastGamificacao").fadeOut(300);
    }, 3500);
}