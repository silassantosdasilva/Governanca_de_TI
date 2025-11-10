// ============================================================
// DASHBOARD DINÂMICO TECHGREEN (FRONT-END DRIVEN)
// ============================================================
// Autor: Silas Santos / TechGreen 2025
// Descrição: Script completo para montar dashboards dinâmicos,
// sem precisar alterar o backend. Ele detecta tabelas, campos,
// renderiza gráficos via Chart.js e registra logs de eventos.
// ============================================================

document.addEventListener("DOMContentLoaded", async () => {
    console.log("🟢 Inicializando Dashboard Dinâmico...");

    const grid = document.querySelector("#dashboard-grid");
    if (!grid) {
        console.error("❌ Elemento #dashboard-grid não encontrado.");
        return;
    }

    // ============================================================
    // 🔹 FUNÇÃO UNIVERSAL DE LOG
    // ============================================================
    async function registrarLog(tipo, mensagem, detalhes = "") {
        try {
            await fetch("/api/LogApi/registrar", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    origem: "Dashboard",
                    tipo: tipo,
                    mensagem: mensagem,
                    detalhes: detalhes
                })
            });
        } catch (err) {
            console.warn("⚠️ Falha ao registrar log:", err);
        }
    }

    // ============================================================
    // 🔹 FUNÇÃO DE CRIAÇÃO VISUAL DO CARD (Widget)
    // ============================================================
    function createWidget(titulo) {
        const card = document.createElement("div");
        card.className = "card shadow-sm rounded-4 dashboard-widget";
        card.innerHTML = `
            <div class="card-header d-flex justify-content-between align-items-center bg-white border-0">
                <h6 class="fw-semibold text-primary mb-0">${titulo || "Novo Widget"}</h6>
                <div class="dropdown">
                    <button class="btn btn-sm btn-light border-0" data-bs-toggle="dropdown">
                        <i class="bi bi-three-dots-vertical"></i>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end shadow-sm">
                        <li><a class="dropdown-item text-primary btn-recarregar" href="#"><i class="bi bi-arrow-repeat me-2"></i> Recarregar</a></li>
                        <li><a class="dropdown-item text-danger btn-remover" href="#"><i class="bi bi-trash3 me-2"></i> Remover</a></li>
                    </ul>
                </div>
            </div>
            <div class="card-body text-center py-4">
                <div class="spinner-border text-primary"></div>
            </div>
        `;

        // Eventos dos botões
        card.querySelector(".btn-recarregar").addEventListener("click", () => {
            const w = JSON.parse(card.dataset.widgetConfig);
            carregarWidget(w.tabela, w.tipo, w.titulo, w.dimensao, w.metrica, w.operacao, card);
        });

        card.querySelector(".btn-remover").addEventListener("click", () => {
            card.remove();
            localStorage.removeItem(`widget-${w.posicao}`);
            registrarLog("Info", `Widget removido: ${titulo}`);
        });

        return card;
    }

    // ============================================================
    // 🔹 RENDERIZAÇÃO DE GRÁFICOS VIA CHART.JS
    // ============================================================
    function renderChart(canvas, tipoGrafico, data) {
        const labels = data.map(d => d.Categoria);
        const valores = data.map(d => d.Valor);

        new Chart(canvas, {
            type: tipoGrafico,
            data: {
                labels: labels,
                datasets: [{
                    label: "Dados",
                    data: valores,
                    borderWidth: 2,
                    borderColor: "#8A2BE2",
                    backgroundColor: [
                        "#8A2BE2", "#7B68EE", "#9370DB",
                        "#BA55D3", "#DDA0DD", "#E6E6FA"
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: tipoGrafico !== "bar" } },
                scales: tipoGrafico === "bar" ? {
                    y: { beginAtZero: true }
                } : {}
            }
        });
    }

    // ============================================================
    // 🔹 FUNÇÃO PRINCIPAL DE CARREGAMENTO DE WIDGET
    // ============================================================
    async function carregarWidget(tabela, tipo, titulo, dimensao, metrica, operacao, cardExistente = null) {
        const widget = cardExistente || createWidget(titulo);
        if (!cardExistente) grid.appendChild(widget);

        const cardBody = widget.querySelector(".card-body");
        cardBody.innerHTML = `<div class="spinner-border text-primary mt-3"></div>`;

        try {
            const resp = await fetch(`/api/DashboardApi/consultar?tabela=${tabela}&tipo=${tipo}&dimensao=${dimensao}&metrica=${metrica}&operacao=${operacao}`);
            if (!resp.ok) throw new Error(`Erro ${resp.status}: ${resp.statusText}`);

            const data = await resp.json();
            cardBody.innerHTML = "";

            if (!data || data.length === 0) {
                cardBody.innerHTML = `<small class="text-muted">Sem dados disponíveis.</small>`;
                return;
            }

            // 🔹 Gráfico ou Lista
            if (["Pizza", "Rolo", "Barra", "Linha"].includes(tipo)) {
                const canvas = document.createElement("canvas");
                canvas.height = 220;
                cardBody.appendChild(canvas);
                const tipoChart = tipo === "Rolo" ? "doughnut"
                    : tipo === "Barra" ? "bar"
                        : tipo === "Linha" ? "line"
                            : "pie";
                renderChart(canvas, tipoChart, data);
            } else if (tipo === "Lista") {
                cardBody.innerHTML = data.map(d => `
                    <div class="d-flex justify-content-between border-bottom small py-1">
                        <span>${d.Categoria}</span>
                        <span class="fw-semibold">${d.Valor}</span>
                    </div>`).join("");
            } else {
                cardBody.innerHTML = `<small class="text-muted">Tipo de widget não reconhecido.</small>`;
            }

            widget.dataset.widgetConfig = JSON.stringify({ tabela, tipo, titulo, dimensao, metrica, operacao });
            registrarLog("Info", `Widget carregado: ${titulo}`);

        } catch (err) {
            console.error("❌ Erro ao carregar widget:", err);
            cardBody.innerHTML = `<div class="text-danger small">Falha ao carregar dados.</div>`;
            await registrarLog("Erro", `Falha ao carregar widget: ${titulo}`, err.message);
        }
    }

    // ============================================================
    // 🔹 CARREGAR TODAS AS TABELAS DISPONÍVEIS (auto)
    // ============================================================
    async function carregarTabelasDisponiveis() {
        try {
            const resp = await fetch("/api/DashboardApi/listar-tabelas");
            const tabelas = await resp.json();
            console.log("📋 Tabelas detectadas:", tabelas);
            return tabelas || [];
        } catch (err) {
            console.error("⚠️ Falha ao buscar tabelas:", err);
            registrarLog("Erro", "Falha ao buscar tabelas", err.message);
            return [];
        }
    }

    // ============================================================
    // 🔹 INICIALIZAÇÃO DOS WIDGETS SALVOS
    // ============================================================
    const widgetsSalvos = Object.keys(localStorage)
        .filter(k => k.startsWith("widget-"))
        .map(k => JSON.parse(localStorage.getItem(k)));

    for (const w of widgetsSalvos) {
        carregarWidget(w.tabela, w.tipo, w.titulo, w.dimensao, w.metrica, w.operacao);
    }

    // ============================================================
    // 🔹 BOTÃO “Adicionar Widget” (detecta nova posição)
    // ============================================================
    document.querySelectorAll(".btn-add-widget").forEach(btn => {
        btn.addEventListener("click", async () => {
            const posicao = btn.dataset.posicao;
            const tabelas = await carregarTabelasDisponiveis();
            const tabela = prompt("Escolha a tabela:\n" + tabelas.join(", "));
            if (!tabela) return;

            const tipo = prompt("Tipo de gráfico (Pizza, Rolo, Barra, Linha, Lista):", "Pizza");
            const titulo = prompt("Título do widget:", `Widget ${tabela}`);
            const dimensao = prompt("Campo de agrupamento (dimensão):", "Status");
            const metrica = prompt("Campo de valor (métrica):", "Id");
            const operacao = prompt("Operação (Contagem, Soma, Média):", "Contagem");

            const novoWidget = { posicao, tabela, tipo, titulo, dimensao, metrica, operacao };
            localStorage.setItem(`widget-${posicao}`, JSON.stringify(novoWidget));
            carregarWidget(tabela, tipo, titulo, dimensao, metrica, operacao);
        });
    });

    console.log("✅ Dashboard Dinâmico pronto!");
});
