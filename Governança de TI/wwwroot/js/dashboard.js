document.addEventListener("DOMContentLoaded", () => {
    const modalEl = document.getElementById("widgetWizardModal");
    const modal = new bootstrap.Modal(modalEl);
    const wizardContainer = document.getElementById("wizardContainer");

    // ===========================================================
    // FUNÇÃO GLOBAL: INICIALIZAÇÃO DO MODAL DE CRIAÇÃO/EDIÇÃO
    // ===========================================================
    function initWidgetWizardModal() {
        console.log("🧠 Wizard inteligente inicializado (v3.2 - parse seguro)");

        const form = document.getElementById("form-widget-config");
        if (!form) {
            console.error("Formulário do wizard (form-widget-config) não encontrado.");
            return;
        }

        // ===========================================================
        // 🔧 PARSE SEGURO DOS JSONs (corrige &quot; e evita erros)
        // ===========================================================
        let camposData = {};
        let regrasValidas = {};

        try {
            const rawCampos = form.dataset.camposJson || "{}";
            const rawRegras = form.dataset.regrasValidas || "{}";

            // Corrige escape HTML (quando o Razor transforma aspas em &quot;)
            const safeCampos = rawCampos.replaceAll("&quot;", '"');
            const safeRegras = rawRegras.replaceAll("&quot;", '"');

            camposData = JSON.parse(safeCampos);
            regrasValidas = JSON.parse(safeRegras);

            console.log("✅ JSONs carregados com sucesso:", {
                camposData,
                regrasValidas
            });
        } catch (e) {
            console.error("❌ Erro ao ler JSON dos campos/regras:", e);
            camposData = {};
            regrasValidas = {};
        }

        // ===========================================================
        // ELEMENTOS HTML
        // ===========================================================
        const selectTipo = document.getElementById("select-tipo-visualizacao");
        const selectTabela = document.getElementById("select-tabela-fonte");
        const selectDimensao = document.getElementById("select-grafico-dimensao");
        const selectMetrica = document.getElementById("select-grafico-metrica");
        const selectOperacao = document.getElementById("select-grafico-operacao");
        const selectFiltroData = document.getElementById("select-filtro-data");

        const grupoKPI = document.getElementById("grupo-kpi");
        const grupoGrafico = document.getElementById("grupo-grafico");
        const grupoGraficoMetrica = document.getElementById("grupo-grafico-metrica");

        // ===========================================================
        // FUNÇÃO: Preenche um dropdown com novos valores
        // ===========================================================
        console.log("📦 REGRAS RECEBIDAS PARA TESTE:", JSON.stringify(regrasValidas, null, 2));

        function fillDropdown(select, items, label) {
            if (!select) return;
            select.innerHTML = `<option value="">${label}</option>`;
            items.forEach(v => {
                const opt = document.createElement("option");
                opt.value = v;
                opt.textContent = v;
                select.appendChild(opt);
            });

            // Fallback visual
            if (items.length === 0) {
                const opt = document.createElement("option");
                opt.disabled = true;
                opt.textContent = "(Sem opções disponíveis)";
                select.appendChild(opt);
            }
        }

        // ===========================================================
        // FUNÇÃO: Atualiza os campos conforme tabela + tipo
        // ===========================================================
        function updateFieldDropdowns() {
            const tabela = selectTabela.value;
            const tipo = selectTipo.value;
            const regras = regrasValidas[tabela] || [];

            if (!tabela) {
                console.warn("⚠️ Nenhuma tabela selecionada.");
                return;
            }

            console.log(`🔄 Atualizando campos: Tabela=${tabela}, Tipo=${tipo}`);

            // Dimensões possíveis
            const dims = [...new Set(regras.filter(r => r.TipoVisualizacao === tipo).map(r => r.Dimensao))];
            fillDropdown(selectDimensao, dims, "-- Selecione Dimensão --");

            // Métricas possíveis
            const mets = [...new Set(regras.filter(r => r.TipoVisualizacao === tipo).map(r => r.Metrica).filter(x => x))];
            fillDropdown(selectMetrica, mets, "-- Selecione Métrica --");

            // Operações possíveis
            const ops = [...new Set(regras.filter(r => r.TipoVisualizacao === tipo).map(r => r.Operacao))];
            fillDropdown(selectOperacao, ops, "-- Selecione Operação --");

            // Campos de data (vindos do JSON de campos)
            const campos = camposData[tabela] || { Datas: [] };
            fillDropdown(selectFiltroData, campos.Datas || [], "-- Nenhum filtro de data --");

            toggleGroups();
        }

        // ===========================================================
        // FUNÇÃO: Mostra/Esconde blocos de acordo com o tipo
        // ===========================================================
        function toggleGroups() {
            const tipo = selectTipo.value;
            const op = selectOperacao.value;

            if (grupoKPI) grupoKPI.style.display = tipo === "Total" ? "block" : "none";
            if (grupoGrafico) grupoGrafico.style.display = ["Pizza", "Barra", "Rolo"].includes(tipo) ? "block" : "none";
            if (grupoGraficoMetrica) grupoGraficoMetrica.style.display = op === "Soma" ? "block" : "none";
        }

        // ===========================================================
        // EVENTOS
        // ===========================================================
        selectTabela?.addEventListener("change", updateFieldDropdowns);
        selectTipo?.addEventListener("change", updateFieldDropdowns);
        selectOperacao?.addEventListener("change", toggleGroups);

        // ===========================================================
        // ESTADO INICIAL
        // ===========================================================
        if (!selectTabela.value && Object.keys(camposData).length > 0) {
            selectTabela.value = Object.keys(camposData)[0]; // seleciona a primeira tabela
        }

        updateFieldDropdowns(); // inicializa campos

        console.log("🧭 Wizard configurado e pronto para uso.");

        // ===========================================================
        // ENVIO DO FORMULÁRIO (AJAX)
        // ===========================================================
        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            const data = new FormData(form);

            try {
                const resp = await fetch(form.action, {
                    method: "POST",
                    body: data,
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });

                const contentType = resp.headers.get("content-type") || "";
                if (contentType.includes("application/json")) {
                    const json = await resp.json();
                    if (json.success) {
                        showFeedbackModal("success", json.message);
                        const instance = bootstrap.Modal.getInstance(modalEl);
                        instance.hide();
                        setTimeout(() => location.reload(), 1000);
                    } else {
                        showFeedbackModal("error", json.message || "Falha ao salvar widget.");
                    }
                } else {
                    const html = await resp.text();
                    wizardContainer.innerHTML = html;
                    initWidgetWizardModal(); // Reinicializa se o HTML for re-renderizado
                }
            } catch (err) {
                console.error("Erro ao enviar widget:", err);
                showFeedbackModal("error", "Erro no envio do formulário.");
            }
        });

        // ===========================================================
        // PRÉ-VISUALIZAÇÃO OPCIONAL
        // ===========================================================
        const btnPreview = document.getElementById("btnPreview");
        const previewContainer = document.getElementById("previewContainer");

        if (btnPreview && previewContainer && form) {
            btnPreview.addEventListener("click", async () => {
                const formData = new FormData(form);
                previewContainer.classList.remove("d-none");
                previewContainer.innerHTML = `
                    <div class="text-center py-4">
                        <div class="spinner-border text-primary" role="status"></div>
                        <div class="small mt-2">Gerando pré-visualização...</div>
                    </div>`;

                try {
                    const resp = await fetch("/DashboardDinamica/PreviewWidget", {
                        method: "POST",
                        body: formData,
                        headers: { "X-Requested-With": "XMLHttpRequest" }
                    });
                    previewContainer.innerHTML = await resp.text();
                } catch (err) {
                    console.error("Erro preview:", err);
                    previewContainer.innerHTML = `
                        <div class="alert alert-danger small text-center">
                            Erro ao gerar pré-visualização.
                        </div>`;
                }
            });
        }

        console.log("✅ Wizard inteligente pronto");
    } // <- Fecha a função initWidgetWizardModal corretamente

    // ===========================================================
    // BOTÃO: ADICIONAR NOVO WIDGET
    // ===========================================================
    document.querySelectorAll(".js-add-widget").forEach(btn => {
        btn.addEventListener("click", async () => {
            const pos = btn.dataset.posicao;
            try {
                const resp = await fetch(`/DashboardDinamica/CriarOuEditar?pos=${pos}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });

                if (!resp.ok) throw new Error(`Erro HTTP ${resp.status}`);
                const html = await resp.text();

                wizardContainer.innerHTML = html;
                initWidgetWizardModal();
                modal.show();
            } catch (err) {
                console.error("Erro ao abrir modal de criação:", err);
                showFeedbackModal("error", "Erro ao abrir assistente de criação do widget.");
            }
        });
    });

    // ===========================================================
    // BOTÃO: EDITAR WIDGET EXISTENTE
    // ===========================================================
    document.addEventListener("click", async e => {
        const el = e.target.closest(".js-edit-widget");
        if (!el) return;

        e.preventDefault();
        const id = el.dataset.id;

        try {
            const resp = await fetch(`/DashboardDinamica/CriarOuEditar?id=${id}`, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!resp.ok) throw new Error(`Erro HTTP ${resp.status}`);
            const html = await resp.text();

            wizardContainer.innerHTML = html;
            initWidgetWizardModal();
            modal.show();
        } catch (err) {
            console.error("Erro ao abrir modal de edição:", err);
            showFeedbackModal("error", "Erro ao abrir assistente de edição do widget.");
        }
    });

    // ===========================================================
    // BOTÃO: EXCLUIR WIDGET
    // ===========================================================
    document.addEventListener("click", async e => {
        const delBtn = e.target.closest(".js-delete-widget");
        if (!delBtn) return;
        e.preventDefault();

        if (!confirm("Deseja realmente excluir este widget?")) return;
        const id = delBtn.dataset.id;

        try {
            const resp = await fetch(`/DashboardDinamica/Excluir?id=${id}`, { method: "POST" });
            const data = await resp.json();

            if (data.success) {
                showFeedbackModal("success", data.message);
                setTimeout(() => location.reload(), 1000);
            } else {
                showFeedbackModal("error", data.message || "Erro ao excluir o widget.");
            }
        } catch (err) {
            console.error("Erro ao excluir widget:", err);
            showFeedbackModal("error", "Falha na comunicação com o servidor.");
        }
    });

    // ===========================================================
    // CARREGAR CONTEÚDO DOS WIDGETS
    // ===========================================================
    document.querySelectorAll(".widget-container").forEach(async (el) => {
        const widgetBody = el.querySelector(".widget-body");
        if (!widgetBody) return;

        const id = el.id.replace("widget-", "");

        try {
            const resp = await fetch(`/DashboardDinamica/CarregarWidget?id=${id}`);
            if (!resp.ok) throw new Error(`Erro HTTP ${resp.status}`);

            const html = await resp.text();
            widgetBody.innerHTML = html;
        } catch (err) {
            console.error("Erro ao carregar widget:", err);
            widgetBody.innerHTML = `<span class="text-danger small">Erro ao carregar widget</span>`;
        }
    });
});
