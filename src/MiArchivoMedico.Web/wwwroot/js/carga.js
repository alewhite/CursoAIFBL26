// Indicador de operación en curso y bloqueo del reenvío (RNF-66), más el aviso de carga interrumpida
// por pérdida de conexión (RF-28). El aviso es del navegador y no del servidor: con la conexión caída
// el servidor no llega a responder nada, así que no hay dónde engancharse del otro lado.
(function () {
    'use strict';

    var formulario = document.getElementById('formulario-de-estudio');
    if (!formulario) return;

    var boton = document.getElementById('boton-guardar');
    var aviso = document.getElementById('aviso-en-curso');
    var enCurso = false;

    formulario.addEventListener('submit', function (evento) {
        if (enCurso) {
            evento.preventDefault();
            return;
        }
        enCurso = true;
        if (boton) boton.disabled = true;
        if (aviso) aviso.hidden = false;
    });

    window.addEventListener('offline', function () {
        if (!enCurso || !aviso) return;
        aviso.textContent = 'Se perdió la conexión: el archivo no se cargó. Volvé a intentarlo.';
        aviso.className = 'aviso error';
        enCurso = false;
        if (boton) boton.disabled = false;
    });
})();
