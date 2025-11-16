// ============================================================
// 🧭 SCRIPT GLOBAL DO WIZARD MODAL (TechGreen Dashboard)
// ============================================================
// v4 - Completo com Busca Distinta e Correções de Erro

window.inicializarWizardModal = function () {
    console.log("⚙️ Inicializando comportamento do Wizard (v4 - Busca Distinta)...");

    // === 1. MAPEAMENTO DE ELEMENTOS ===
    const steps = document.querySelectorAll(".wizard-step");
    const btnPrev = document.getElementById("btnPrev");
    const btnNext = document.getElementById("btnNext");
    const btnFinish = document.getElementById("btnFinish");
    const wizardForm = document.getElementById("widgetWizardForm");

    // Elementos PASSO 1
    const titulo = document.getElementById("tituloWidget");
    const tipo = document.getElementById("tipoVisualizacao");

    // Elementos PASSO 2
    const tabelaSelect = document.getElementById("tabelaFonte");

    // Elementos PASSO 3
    const blocoAgregacao = document.getElementById("bloco-agregacao");
    const blocoFiltroLista = document.getElementById("bloco-filtro-lista");

    // Campos de Agregação
    const operacao = document.getElementById("operacaoWidget");
    const campoMetrica = document.getElementById("campoMetrica");
    const campoDimensao = document.getElementById("campoDimensao");

    // Campos de Filtro (Novos)
    const campoFiltro = document.getElementById("campoFiltro");

    // *** ESTA É A CORREÇÃO DO ERRO 'ReferenceError' ***
    const valorFiltroContainer = document.getElementById("valor-filtro-container");

    // Campos de Data
    const campoDataFiltro = document.getElementById("campoDataFiltro");
    const dataInicio = document.getElementById("dataInicio");
    const dataFim = document.getElementById("dataFim");

    let currentStep = 0;
    let dashboardSchema = {};

    // ============================================================
    // === 2. NAVEGAÇÃO DO WIZARD ===
    // ============================================================
    const showStep = (i) => {
        steps.forEach((s, idx) => s.classList.toggle("d-none", idx !== i));
        btnPrev.disabled = i === 0;
        btnNext.classList.toggle("d-none", i === steps.length - 1);
        btnFinish.classList.toggle("d-none", i !== steps.length - 1);
    };
    showStep(0);

    btnNext.onclick = () => {
        if (currentStep < steps.length - 1) currentStep++;
        showStep(currentStep);
    };
    btnPrev.onclick = () => {
        if (currentStep > 0) currentStep--;
        showStep(currentStep);
    };

    // ============================================================
    // === 3. PREENCHIMENTO INICIAL DO TIPO ===
    // ============================================================
    tipo.innerHTML = `
        <option value="">-- Selecione --</option>
        <option value="Total">Total (KPI)</option>
        <option value="Pizza">Gráfico de Pizza</option>
        <option value="Rolo">Gráfico de Rosca</option>
        <option value="Barra">Gráfico de Barra</option>
        <option value="Linha">Gráfico de Linha</option>
        <option value="Lista">Lista / Tabela</option>
    `;

    // ============================================================
    // === 4. CARREGAR SCHEMA DA API ===
    // ============================================================
    async function carregarTabelas() {
        try {
            tabelaSelect.innerHTML = "<option>Carregando tabelas...</option>";
            const resp = await fetch("/api/dashboard/schema");
            if (!resp.ok) throw new Error(`Erro HTTP ${resp.status}`);

            const schema = await resp.json();
            dashboardSchema = schema;
            console.log("📊 Schema recebido da API:", schema);

            const tabelas = Object.keys(schema);
            if (!tabelas.length) {
                tabelaSelect.innerHTML = "<option value=''>Nenhuma tabela encontrada</option>";
                return;
            }

            tabelaSelect.innerHTML = "<option value=''>-- Selecione uma tabela --</option>";
            tabelas.forEach(t => {
                tabelaSelect.insertAdjacentHTML("beforeend", `<option value="${t}">${t}</option>`);
            });
        } catch (err) {
            tabelaSelect.innerHTML = "<option value=''>Erro ao carregar tabelas</option>";
            console.error("❌ Erro ao carregar schema:", err);
        }
    }

    // Função auxiliar para normalizar nomes
    const normalizar = (obj, nome) => {
        const chave = Object.keys(obj).find(k => k.toLowerCase() === nome.toLowerCase());
        return chave ? obj[chave] : [];
    };

    // ============================================================
    // === 5. LISTENERS DE EVENTO (MUDANÇA DE CAMPOS) ===
    // ============================================================

    // Quando o usuário MUDA A TABELA
    tabelaSelect.addEventListener("change", () => {
        const tabela = tabelaSelect.value;
        if (!tabela || !dashboardSchema[tabela]) return;

        const meta = dashboardSchema[tabela];
        console.log("📋 Campos da tabela selecionada:", meta);

        // Preenche Agregação (Dimensão, Métrica)
        const metricas = normalizar(meta, "CamposMetrica");
        const dimensoes = normalizar(meta, "CamposDimensao");
        const datas = normalizar(meta, "CamposData");
        const filtros = normalizar(meta, "CamposFiltro"); // Pega a nova lista

        campoMetrica.innerHTML = "<option value=''>-- Campo Métrica --</option>";
        campoDimensao.innerHTML = "<option value=''>-- Campo Dimensão --</option>";
        campoDataFiltro.innerHTML = "<option value=''>-- Nenhum filtro --</option>";
        campoFiltro.innerHTML = "<option value=''>-- Campo de Filtro --</option>";

        metricas.forEach(c => campoMetrica.insertAdjacentHTML("beforeend", `<option value="${c}">${c}</option>`));
        dimensoes.forEach(c => campoDimensao.insertAdjacentHTML("beforeend", `<option value="${c}">${c}</option>`));
        datas.forEach(c => campoDataFiltro.insertAdjacentHTML("beforeend", `<option value="${c}">${c}</option>`));
        filtros.forEach(c => campoFiltro.insertAdjacentHTML("beforeend", `<option value="${c}">${c}</option>`));

        // Reseta o campo de valor
        reverterParaTextInput();
    });

    // Quando o usuário MUDA O TIPO DE VISUALIZAÇÃO (Lista vs Gráfico)
    tipo.addEventListener("change", () => {
        const tipoSelecionado = tipo.value;

        if (tipoSelecionado === "Lista") {
            // É uma Lista: mostra Bloco de Filtro, esconde Bloco de Agregação
            blocoAgregacao.classList.add("d-none");
            blocoFiltroLista.classList.remove("d-none");
        } else {
            // É Gráfico ou KPI: mostra Bloco de Agregação, esconde Bloco de Filtro
            blocoAgregacao.classList.remove("d-none");
            blocoFiltroLista.classList.add("d-none");
        }
    });

    // ============================================================
    // === 6. LÓGICA DE BUSCA DE VALORES DISTINTOS ===
    // ============================================================

    // Função auxiliar para reverter para o input de texto
    function reverterParaTextInput() {
        if (valorFiltroContainer) { // Checagem de segurança
            valorFiltroContainer.innerHTML = `
                <label class="form-label fw-semibold">Valor</label>
                <input type="text" id="valorFiltro" class="form-control" placeholder="Digite o valor" />
            `;
        }
    }

    // Quando o usuário MUDA O CAMPO DE FILTRO (ex: seleciona "Status")
    campoFiltro.addEventListener("change", async () => {
        const campoSelecionado = campoFiltro.value;
        const tabelaSelecionada = tabelaSelect.value;

        if (!campoSelecionado || !tabelaSelecionada) {
            reverterParaTextInput();
            return;
        }

        // Mostra o "Carregando..."
        valorFiltroContainer.innerHTML = `
            <label class="form-label fw-semibold">Valor</label>
            <select id="valorFiltro" class="form-select" disabled>
                <option>Carregando valores...</option>
            </select>
        `;

        try {
            const response = await fetch("/api/dashboard/distinct-values", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    Tabela: tabelaSelecionada,
                    Campo: campoSelecionado
                })
            });

            if (!response.ok) throw new Error("Falha na API");

            const valores = await response.json();

            // Se a API não retornar valores (ex: campo 'Descricao' ou erro)
            if (!Array.isArray(valores) || valores.length === 0) {
                reverterParaTextInput();
                return;
            }

            // Constrói o novo <select> com os valores
            let optionsHtml = '<option value="">-- Selecione o valor --</option>';
            valores.forEach(valor => {
                const val = valor || "N/A"; // Trata valores nulos
                optionsHtml += `<option value="${val}">${val}</option>`;
            });

            valorFiltroContainer.innerHTML = `
                <label class="form-label fw-semibold">Valor</label>
                <select id="valorFiltro" class="form-select">
                    ${optionsHtml}
                </select>
            `;

        } catch (err) {
            console.error("Falha ao buscar valores distintos:", err);
            reverterParaTextInput(); // Falhou? Reverte para o input de texto
        }
    });

    // ============================================================
    // === 7. LÓGICA DE SALVAMENTO (SUBMIT) ===
    // ============================================================
    wizardForm.addEventListener("submit", async (e) => {
        e.preventDefault();

        // *** ESTA É A CORREÇÃO DO BUG DE SUBMIT ***
        // Buscamos o elemento 'valorFiltro' NO MOMENTO do submit,
        // pois ele pode ser um <input> ou <select>
        const valorFiltroAtual = document.getElementById("valorFiltro");

        const widgetConfig = {
            // Info principal (em camelCase para o localStorage)
            titulo: titulo.value,
            tipo: tipo.value,
            tabela: tabelaSelect.value,

            // Agregação
            operacao: operacao.value,
            metrica: campoMetrica.value,
            dimensao: campoDimensao.value,

            // Filtros de Data
            dataInicio: dataInicio.value ? dataInicio.value : null,
            dataFim: dataFim.value ? dataFim.value : null,

            // Filtro WHERE (lendo o valor dinâmico)
            filtroCampo: campoFiltro.value ? campoFiltro.value : null,
            filtroValor: valorFiltroAtual ? valorFiltroAtual.value : null, // Usa a variável dinâmica

            // Metadado do frontend
            posicao: Date.now()
        };

        if (!widgetConfig.titulo || !widgetConfig.tabela || !widgetConfig.tipo) {
            alert("⚠️ Preencha os campos obrigatórios (Título, Tabela e Tipo)!");
            return;
        }

        // Pega widgets existentes e adiciona o novo
        const widgetsExistentes = JSON.parse(localStorage.getItem("widgetsDashboard") || "[]");
        widgetsExistentes.push(widgetConfig);
        localStorage.setItem("widgetsDashboard", JSON.stringify(widgetsExistentes));

        // Fecha o modal
        const modal = bootstrap.Modal.getInstance(document.querySelector("#widgetWizardModal"));
        modal.hide();

        alert("✅ Widget salvo com sucesso!");
        window.dispatchEvent(new Event("widgetsAtualizados")); // Notifica a Index.cshtml
    });

    // ============================================================
    // === 8. INICIALIZAÇÃO ===
    // ============================================================
    carregarTabelas();
};