// ============================================================
// 📜 Logs.js — controle da tela de Logs do Sistema
// ============================================================
//
// - Limpa filtros do formulário
// - Apaga todos os logs via AJAX
// - Recarrega a página após exclusão
// ============================================================

document.addEventListener("DOMContentLoaded", () => {
    const btnApagar = document.getElementById("btnApagar");
    const btnLimpar = document.getElementById("btnLimpar");

    // 🧹 Limpar filtros
    btnLimpar?.addEventListener("click", () => {
        document.querySelectorAll("#filtroForm input").forEach(i => i.value = "");
        document.getElementById("filtroForm").submit();
    });

    // 🗑️ Apagar todos os logs
    btnApagar?.addEventListener("click", async () => {
        if (!confirm("Deseja realmente apagar todos os logs?")) return;

        try {
            const res = await fetch("/Logs/ApagarTudo", {
                method: "POST",
                headers: { "Content-Type": "application/json" }
            });

            const data = await res.json();
            alert(data.message || "Ação concluída.");
            if (data.success) location.reload();
        } catch (err) {
            alert("Erro ao tentar apagar logs.");
            console.error("Erro:", err);
        }
    });
});
