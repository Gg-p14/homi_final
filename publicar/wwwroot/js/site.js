(function () {
    "use strict";

    var raiz = document.documentElement;

    // Cambiar tema claro/oscuro (la preferencia guardada ya se aplica en el <head>).
    var botonTema = document.getElementById("cambiar-tema");
    if (botonTema) {
        botonTema.addEventListener("click", function () {
            var nuevo = raiz.getAttribute("data-theme") === "dark" ? "light" : "dark";
            raiz.setAttribute("data-theme", nuevo);
            try { localStorage.setItem("homi-tema", nuevo); } catch (e) { }
        });
    }

    // Menú del icono de usuario.
    var botonUsuario = document.getElementById("boton-usuario");
    var desplegable = document.getElementById("usuario-desplegable");
    if (botonUsuario && desplegable) {
        botonUsuario.addEventListener("click", function (e) {
            e.stopPropagation();
            var abrir = desplegable.hidden;
            desplegable.hidden = !abrir;
            botonUsuario.setAttribute("aria-expanded", String(abrir));
        });
        document.addEventListener("click", function (e) {
            if (!desplegable.contains(e.target) && e.target !== botonUsuario) {
                desplegable.hidden = true;
                botonUsuario.setAttribute("aria-expanded", "false");
            }
        });
    }

    // Confirmación Sí / No antes de enviar formularios con data-confirmar.
    var modal = document.getElementById("modal-confirmar");
    var formularioPendiente = null;

    function cerrarModal() {
        if (modal) modal.hidden = true;
        formularioPendiente = null;
    }

    if (modal) {
        modal.querySelector("[data-no]").addEventListener("click", cerrarModal);
        modal.querySelector("[data-si]").addEventListener("click", function () {
            var formulario = formularioPendiente;
            cerrarModal();
            if (formulario) formulario.submit();
        });
        modal.addEventListener("click", function (e) {
            if (e.target === modal) cerrarModal();
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !modal.hidden) cerrarModal();
        });
    }

    document.querySelectorAll("form[data-confirmar]").forEach(function (formulario) {
        formulario.addEventListener("submit", function (e) {
            if (formulario.dataset.confirmado === "1") return;
            e.preventDefault();
            formularioPendiente = formulario;
            modal.querySelector("#modal-texto").textContent = formulario.getAttribute("data-confirmar");
            modal.hidden = false;
        });
    });

    // Requisitos y fortaleza de la contraseña en el registro.
    var campoPassword = document.querySelector("input[data-fortaleza]");
    var barra = document.getElementById("fortaleza-barra");
    var texto = document.getElementById("fortaleza-texto");
    var requisitos = document.querySelectorAll("#requisitos-password li");

    var reglas = {
        longitud: function (v) { return v.length >= 8; },
        mayuscula: function (v) { return /[A-Z]/.test(v); },
        numero: function (v) { return /\d/.test(v); },
        especial: function (v) { return /[^A-Za-z0-9]/.test(v); }
    };

    if (campoPassword && barra && texto) {
        var niveles = [
            { nombre: "Muy débil", ancho: "20%", color: "#d70015" },
            { nombre: "Débil", ancho: "40%", color: "#ff9f0a" },
            { nombre: "Aceptable", ancho: "60%", color: "#ffd60a" },
            { nombre: "Buena", ancho: "80%", color: "#30d158" },
            { nombre: "Muy buena", ancho: "100%", color: "#1f8a4c" }
        ];

        campoPassword.addEventListener("input", function () {
            var valor = campoPassword.value;

            requisitos.forEach(function (li) {
                var regla = reglas[li.getAttribute("data-regla")];
                li.classList.toggle("cumple", regla(valor));
            });

            if (valor.length === 0) {
                barra.style.width = "0%";
                texto.textContent = "";
                return;
            }

            var puntos = 0;
            if (valor.length >= 8) puntos++;
            if (valor.length >= 12) puntos++;
            if (/\d/.test(valor)) puntos++;
            if (/[a-z]/.test(valor) && /[A-Z]/.test(valor)) puntos++;
            if (/[^A-Za-z0-9]/.test(valor)) puntos++;

            var nivel = niveles[Math.min(Math.max(puntos - 1, 0), niveles.length - 1)];
            barra.style.width = nivel.ancho;
            barra.style.background = nivel.color;
            texto.textContent = "Fortaleza: " + nivel.nombre;
        });
    }

    // Cuenta regresiva del pago.
    document.querySelectorAll("[data-cuenta-regresiva]").forEach(function (elemento) {
        var segundos = parseInt(elemento.getAttribute("data-segundos"), 10) || 0;

        function pintar() {
            var minutos = Math.floor(segundos / 60);
            var resto = segundos % 60;
            elemento.textContent = String(minutos).padStart(2, "0") + ":" + String(resto).padStart(2, "0");
        }

        pintar();
        var temporizador = setInterval(function () {
            segundos--;
            pintar();
            if (segundos <= 0) {
                clearInterval(temporizador);
                elemento.textContent = "Tiempo agotado";
                setTimeout(function () { location.reload(); }, 1500);
            }
        }, 1000);
    });

    // Botón imprimir (comprobante).
    document.querySelectorAll("[data-imprimir]").forEach(function (boton) {
        boton.addEventListener("click", function () { window.print(); });
    });
})();


// Campos de solo dígitos (teléfono celular de 10): descarta letras y corta al llegar a 10.
(function () {
    document.querySelectorAll("input[data-solo-digitos]").forEach(function (campo) {
        var max = parseInt(campo.getAttribute("maxlength") || "10", 10);
        campo.addEventListener("input", function () {
            var limpio = campo.value.replace(/\D/g, "").slice(0, max);
            if (campo.value !== limpio) campo.value = limpio;
        });
    });
})();

// Ver la foto de perfil en grande (cualquier imagen con data-ampliar).
(function () {
    var visor = null;

    function cerrar() {
        if (visor) visor.hidden = true;
    }

    function abrir(src) {
        if (!visor) {
            visor = document.createElement("div");
            visor.className = "visor-foto";
            visor.hidden = true;
            visor.innerHTML = '<img alt="Foto de perfil ampliada" /><button type="button" class="visor-cerrar" aria-label="Cerrar">×</button>';
            document.body.appendChild(visor);
            visor.addEventListener("click", cerrar);
            document.addEventListener("keydown", function (e) { if (e.key === "Escape") cerrar(); });
        }
        visor.querySelector("img").src = src;
        visor.hidden = false;
    }

    // Fase de captura: la foto abre el visor sin abrir también el menú.
    document.addEventListener("click", function (e) {
        var imagen = e.target.closest ? e.target.closest("[data-ampliar]") : null;
        if (!imagen) return;
        e.preventDefault();
        e.stopPropagation();
        abrir(imagen.getAttribute("src"));
    }, true);
})();

