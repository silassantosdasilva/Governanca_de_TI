// ============================================================
// 📊 Dashboard Status Monitor — Exibe logs e status do sistema
// ============================================================

document.addEventListener("DOMContentLoaded", () => {
    const statusBody = document.getElementById("status-body");
    const btnVerLogs = document.getElementById("btnVerLogs");

    async function carregarStatus() {
        try {
            const res = await fetch("/api/dashboard/status");
            const data = await res.json();

            if (data.Critico) {
                statusBody.innerHTML = `
                    <div class="alert alert-danger py-2 mb-2">
                        <i class="bi bi-exclamation-octagon me-2"></i>${data.Status}
                    </div>
                `;
            } else {
                statusBody.innerHTML = `
                    <div class="alert alert-success py-2 mb-2">
                        <i class="bi bi-check-circle me-2"></i>${data.Status}
                    </div>
                `;
            }

            const logsHtml = data.Logs.map(l => `
                <div class="border-bottom py-1 small">
                    <strong>[${new Date(l.DataRegistro).toLocaleTimeString()}]</strong>
                    <span class="${l.Tipo === 'Erro' ? 'text-danger' : 'text-secondary'}">${l.Tipo}</span> —
                    <span>${l.Mensagem}</span>
                </div>
            `).join('');

            statusBody.innerHTML += `<div>${logsHtml || '<em>Nenhum log recente</em>'}</div>`;
        } catch (err) {
            statusBody.innerHTML = `<div class="alert alert-warning small">Erro ao carregar status.</div>`;
            console.error("Erro ao obter status do sistema:", err);
        }
    }

    // 🔁 Atualiza status a cada 60 segundos
    carregarStatus();
    setInterval(carregarStatus, 60000);

    // 🔗 Botão "Ver Logs"
    btnVerLogs?.addEventListener("click", () => {
        window.open("/Logs", "_blank");
    });
});
