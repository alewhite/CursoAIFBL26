// Aviso de carga interrumpida por falta de conexión (RF-28, AC-42).
//
// Por qué acá y no en el servidor: cuando la conexión se corta durante el envío, el servidor no llega a
// responder nada —y si el cuerpo llega truncado, el 400 lo produce la capa HTTP por debajo de la
// aplicación, sin cuerpo—. El único que sabe que el envío no se completó es el navegador, así que es el
// único que puede decirlo sin inventar.
//
// Es una mejora progresiva: sin JavaScript el formulario se envía como siempre.
(function () {
    'use strict';

    if (!window.fetch || !window.FormData) {
        return;
    }

    var MENSAJE_SIN_CONEXION =
        'El archivo no fue cargado: no hay conexión. No se guardó ningún cambio; ' +
        'volvé a intentarlo cuando la conexión se restablezca.';

    document.querySelectorAll('form[data-aviso-de-carga]').forEach(function (formulario) {
        var aviso = formulario.querySelector('[data-aviso-de-carga-mensaje]');
        var boton = formulario.querySelector('button[type="submit"]');

        formulario.addEventListener('submit', function (evento) {
            evento.preventDefault();
            ocultarAviso();

            // navigator.onLine solo es confiable cuando dice que no hay red, que es justo el caso que
            // permite evitar el envío en lugar de esperar a que falle.
            if (navigator.onLine === false) {
                mostrarAviso();
                return;
            }

            bloquear(true);

            fetch(formulario.action, {
                method: 'POST',
                body: new FormData(formulario),
                credentials: 'same-origin',
                redirect: 'follow'
            }).then(function (respuesta) {
                if (respuesta.redirected) {
                    // Alta o edición exitosa: el servidor redirige al detalle del estudio.
                    window.location.assign(respuesta.url);
                    return;
                }

                // El servidor devolvió el formulario con sus errores de validación. Se reemplaza el
                // documento entero para que la página quede igual que en un envío normal, con los
                // mensajes junto a cada campo y los scripts de validación en funcionamiento.
                return respuesta.text().then(function (html) {
                    document.open();
                    document.write(html);
                    document.close();
                });
            }).catch(function () {
                // Única causa posible acá: la solicitud no llegó a completarse.
                bloquear(false);
                mostrarAviso();
            });
        });

        function bloquear(activo) {
            if (boton) {
                boton.disabled = activo;
            }
        }

        function mostrarAviso() {
            if (!aviso) {
                return;
            }

            aviso.textContent = MENSAJE_SIN_CONEXION;
            aviso.hidden = false;
            aviso.focus();
        }

        function ocultarAviso() {
            if (aviso) {
                aviso.hidden = true;
            }
        }
    });
})();
