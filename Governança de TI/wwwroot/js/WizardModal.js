// ============================================================
// 🧭 SCRIPT GLOBAL DO WIZARD MODAL (TechGreen Dashboard)
// ============================================================
// Esta função é executada quando o modal é carregado via fetch
// ============================================================

window.inicializarWizardModal = function () {
    console.log("⚙️ Inicializando comportamento do Wizard...");

    const steps = document.querySelectorAll(".wizard-step");
    const btnPrev = document.getElementById("btnPrev");
    const btnNext = document.getElementById("btnNext");
    const btnFinish = document.getElementById("btnFinish");

    const titulo = document.getElementById("tituloWidget");
    const tipo = document.getElementById("tipoVisualizacao");
    const tabelaSelect = document.getElementById("tabelaFonte");
    const campoMetrica = document.getElementById("campoMetrica");
    const campoDimensao = document.getElementById("campoDimensao");
    const campoDataFiltro = document.getElementById("campoDataFiltro");
    const operacao = document.getElementById("operacaoWidget");

    let currentStep = 0;
    let dashboardSchema = {};

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
    // 🔹 Preenche o select de tipo de visualização
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
    // 🔹 Carregar tabelas do backend
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

            console.log("✅ Tabelas carregadas:", tabelas);
        } catch (err) {
            tabelaSelect.innerHTML = "<option value=''>Erro ao carregar tabelas</option>";
            console.error("❌ Erro ao carregar schema:", err);
        }
    }


    tabelaSelect.addEventListener("change", () => {
        const tabela = tabelaSelect.value;
        if (!tabela || !dashboardSchema[tabela]) return;

        const meta = dashboardSchema[tabela];
        console.log("📋 Campos da tabela selecionada:", meta);

        // Normaliza nomes de propriedades para evitar variações
        const normalizar = (obj, nome) => {
            const chave = Object.keys(obj).find(k => k.toLowerCase() === nome.toLowerCase());
            return chave ? obj[chave] : [];
        };

        const metricas = normalizar(meta, "CamposMetrica");
        const dimensoes = normalizar(meta, "CamposDimensao");
        const datas = normalizar(meta, "CamposData");

        campoMetrica.innerHTML = "<option value=''>-- Campo Métrica --</option>";
        campoDimensao.innerHTML = "<option value=''>-- Campo Dimensão --</option>";
        campoDataFiltro.innerHTML = "<option value=''>-- Nenhum filtro --</option>";

        metricas.forEach(c => campoMetrica.insertAdjacentHTML("beforeend", `<option value="${c}">${c}</option>`));
        dimensoes.forEach(c => campoDimensao.insertAdjacentHTML("beforeend", `<option value="${c}">${c}</option>`));
        datas.forEach(c => campoDataFiltro.insertAdjacentHTML("beforeend", `<option value="${c}">${c}</option>`));
    });


    // 🔹 Salvamento do widget no localStorage
    // ============================================================
    document.getElementById("widgetWizardForm").addEventListener("submit", async (e) => {
        e.preventDefault();

        const widgetConfig = {
            posicao: Date.now(), // usa timestamp p/ evitar sobrescrever
            tabela: tabelaSelect.value,
            tipo: tipo.value,
            titulo: titulo.value,
            dimensao: campoDimensao.value,
            metrica: campoMetrica.value,
            operacao: operacao.value
        };

        if (!widgetConfig.titulo || !widgetConfig.tabela || !widgetConfig.tipo) {
            alert("⚠️ Preencha os campos obrigatórios!");
            return;
        }

        // Pega widgets existentes e adiciona o novo
        const widgetsExistentes = JSON.parse(localStorage.getItem("widgetsDashboard") || "[]");
        widgetsExistentes.push(widgetConfig);
        localStorage.setItem("widgetsDashboard", JSON.stringify(widgetsExistentes));

        const modal = bootstrap.Modal.getInstance(document.querySelector("#widgetWizardModal"));
        modal.hide();

        alert("✅ Widget salvo com sucesso!");
        window.dispatchEvent(new Event("widgetsAtualizados")); // notifica Index
    });

    carregarTabelas();
};
