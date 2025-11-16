// ========================================================================
// VALIDAÇÃO FRONT-END GLOBAL (TechGreen 2025)
// - Valida todos os formulários automaticamente
// - Bloqueia submit se existirem campos obrigatórios vazios
// - Exibe toast bonito no topo
// ========================================================================

document.addEventListener("submit", function (e) {

    const form = e.target;

    // 🔸 Pega todos os campos obrigatórios
    const camposObrigatorios = form.querySelectorAll("[required]");

    let erros = [];

    camposObrigatorios.forEach(campo => {

        // Remove espaços para validação mais justa
        const valor = campo.value ? campo.value.trim() : "";

        if (valor === "") {

            // Pega o label automaticamente
            const label = form.querySelector(`label[for="${campo.id}"]`);

            const nomeCampo =
                label?.innerText?.replace("*", "")?.trim() ||
                campo.getAttribute("name") ||
                "Campo obrigatório";

            erros.push(nomeCampo);
        }
    });

    // Se tiver erros, bloqueia o submit
    if (erros.length > 0) {
        e.preventDefault();
        e.stopPropagation();

        mostrarToastErro("Preencha os campos obrigatórios:\n• " + erros.join("\n• "));

        return false;
    }
});

// ========================================================================
// TOAST GLOBAL
// ========================================================================
function mostrarToastErro(msg) {
    const toast = document.createElement("div");
    toast.className = "toast align-items-center text-white bg-danger border-0";
    toast.role = "alert";
    toast.style.position = "fixed";
    toast.style.top = "20px";
    toast.style.right = "20px";
    toast.style.zIndex = "99999";

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${msg.replace(/\n/g, "<br>")}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" 
                    data-bs-dismiss="toast"></button>
        </div>
    `;

    document.body.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    toast.addEventListener("hidden.bs.toast", () => toast.remove());
}
