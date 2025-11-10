// ============================================================
// 📊 GRÁFICO DE LOGS – ERROS × TEMPO
// ============================================================

document.addEventListener("DOMContentLoaded", async () => {
    const container = document.getElementById("grafico-logs-container");
    if (!container) return;

    container.innerHTML = `
        <div class="text-center text-muted py-3">
            <div class="spinner-border text-primary" role="status"></div>
            <p class="small mt-2">Carregando gráfico de logs...</p>
        </div>
    `;

    try {
        const resp = await fetch("/api/dashboard/logs-grafico");
        const data = await resp.json();

        // Limpa o container
        container.innerHTML = `<canvas id="graficoLogs" height="150"></canvas>`;
        const ctx = document.getElementById("graficoLogs").getContext("2d");

        new Chart(ctx, {
            type: "bar",
            data: {
                labels: data.map(x => x.Dia),
                datasets: [
                    { label: "Erros", data: data.map(x => x.Erro), backgroundColor: "rgba(220,53,69,0.7)" },
                    { label: "Avisos", data: data.map(x => x.Aviso), backgroundColor: "rgba(255,193,7,0.7)" },
                    { label: "Informações", data: data.map(x => x.Info), backgroundColor: "rgba(13,110,253,0.7)" }
                ]
            },
            options: {
                responsive: true,
                scales: { y: { beginAtZero: true } },
                plugins: { legend: { position: "bottom" } }
            }
        });
    } catch (err) {
        console.error("Erro ao carregar gráfico de logs:", err);
        container.innerHTML = `<div class="alert alert-danger small">Erro ao carregar gráfico de logs.</div>`;
    }
});
