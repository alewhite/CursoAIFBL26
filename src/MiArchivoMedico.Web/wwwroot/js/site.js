// Registro del service worker (RF-24, RF-26). Sin él, la aplicación sigue funcionando igual: lo único
// que se pierde es la instalación y la pantalla controlada sin conexión.
if ('serviceWorker' in navigator) {
    window.addEventListener('load', function () {
        navigator.serviceWorker.register('/sw.js').catch(function () {
            // Un registro fallido no debe romper la navegación ni ensuciar la consola del usuario.
        });
    });
}
