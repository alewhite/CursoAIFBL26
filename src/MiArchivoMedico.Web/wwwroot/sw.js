// Service worker propio, con una única regla, para poder leerlo entero (RNF-51).
//
// Solo entra en la caché lo que declara ESTATICOS: archivos estáticos y la pantalla sin conexión.
// Ahí no hay ninguna ruta de la aplicación. Las navegaciones y la entrega de archivos son red o
// pantalla sin conexión, sin nada intermedio, y NUNCA se guarda una respuesta de la red: cachear
// páginas «para que ande offline» dejaría información médica al alcance de quien abra el navegador
// sin sesión.

const VERSION = 'archivo-medico-v1';

const ESTATICOS = [
  '/sin-conexion',
  '/css/sitio.css',
  '/js/carga.js',
  '/manifest.webmanifest',
  '/iconos/icono-192.png',
  '/iconos/icono-512.png',
];

self.addEventListener('install', function (evento) {
  evento.waitUntil(
    caches.open(VERSION)
      .then(function (cache) { return cache.addAll(ESTATICOS); })
      .then(function () { return self.skipWaiting(); })
  );
});

self.addEventListener('activate', function (evento) {
  evento.waitUntil(
    caches.keys()
      .then(function (nombres) {
        return Promise.all(nombres
          .filter(function (nombre) { return nombre !== VERSION; })
          .map(function (nombre) { return caches.delete(nombre); }));
      })
      .then(function () { return self.clients.claim(); })
  );
});

self.addEventListener('fetch', function (evento) {
  const solicitud = evento.request;

  if (solicitud.method !== 'GET') return;

  // Una navegación va siempre a la red. Si no hay conexión, se muestra la pantalla controlada, que no
  // contiene ningún dato médico. La respuesta de la red no se guarda nunca.
  if (solicitud.mode === 'navigate') {
    evento.respondWith(
      fetch(solicitud).catch(function () { return caches.match('/sin-conexion'); })
    );
    return;
  }

  // Para lo demás, se responde desde la caché solo si esa entrada es una de las precargadas.
  const url = new URL(solicitud.url);
  if (url.origin === self.location.origin && ESTATICOS.indexOf(url.pathname) !== -1) {
    evento.respondWith(
      caches.match(url.pathname).then(function (guardada) { return guardada || fetch(solicitud); })
    );
  }
});
