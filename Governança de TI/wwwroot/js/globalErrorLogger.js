window.onerror = function (msg, url, line, col, error) {
    try {
        fetch("/api/log/registrar", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                origem: "Frontend-JS",
                tipo: "Erro",
                mensagem: msg?.toString(),
                detalhes: JSON.stringify({
                    url,
                    line,
                    col,
                    stack: error?.stack
                })
            })
        });
    } catch { }
};

window.addEventListener("unhandledrejection", function (event) {
    try {
        fetch("/api/log/registrar", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                origem: "Frontend-Promise",
                tipo: "Erro",
                mensagem: event.reason?.message || "Unhandled Promise Rejection",
                detalhes: JSON.stringify({
                    stack: event.reason?.stack
                })
            })
        });
    } catch { }
});