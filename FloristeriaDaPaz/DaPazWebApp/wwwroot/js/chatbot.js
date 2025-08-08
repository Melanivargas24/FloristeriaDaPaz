document.addEventListener("DOMContentLoaded", function () {
    const btnOpen = document.getElementById("chatbot-button");
    const btnClose = document.getElementById("chatbot-close");
    const popup = document.getElementById("chatbot-popup");
    const messages = document.getElementById("chatbot-messages");
    const input = document.getElementById("chatbot-input");
    const btnSend = document.getElementById("chatbot-send");

    let chatIniciado = false;

    // Función para mostrar mensajes en el chat
    function mostrarMensaje(remitente, texto) {
        const div = document.createElement("div");
        div.classList.add("d-flex", "align-items-center", "mb-2");

        if (remitente === "bot") {
            div.classList.add("justify-content-start");
            div.innerHTML = `
            <img src="/imagenes/Rosita.png" alt="Rosita" style="width:32px; height:32px; border-radius:50%; margin-right:10px;" />
            <div class="badge bg-secondary text-wrap" style="max-width: 75%;">${texto}</div>
        `;
        } else {
            div.classList.add("justify-content-end");
            div.innerHTML = `
            <div class="badge bg-primary text-wrap" style="max-width: 75%;">${texto}</div>
        `;
        }

        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
    }

    // Enviar mensaje al servidor
    function enviarMensaje(mensaje) {
        fetch("/Chatbot/EnviarMensaje", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ mensaje: mensaje })
        })
            .then(res => res.json())
            .then(data => {
                mostrarMensaje("bot", data.respuesta);
            })
            .catch(err => console.error("Error:", err));
    }

    // Abrir el chat y enviar mensaje predeterminado la primera vez
    btnOpen.addEventListener("click", function () {
        popup.style.display = "flex";

        if (!chatIniciado) {
            enviarMensaje("Eres el asistente virtual de la Floristería Da'Paz, tu nombre para estas consulta de ahora en adelante va a ser Rosita. Ayuda al cliente a elegir flores, arreglos y resolver dudas.");
            chatIniciado = true;
        }
    });

    // Cerrar chat
    btnClose.addEventListener("click", function () {
        popup.style.display = "none";
    });

    // Enviar mensaje del usuario
    btnSend.addEventListener("click", function () {
        const texto = input.value.trim();
        if (texto) {
            mostrarMensaje("usuario", texto);
            enviarMensaje(texto);
            input.value = "";
        }
    });

    // Enviar con Enter
    input.addEventListener("keypress", function (e) {
        if (e.key === "Enter") {
            btnSend.click();
        }
    });
});
