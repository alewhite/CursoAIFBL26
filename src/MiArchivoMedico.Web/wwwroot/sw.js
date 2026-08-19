// Service worker de Mi Archivo Médico (RF-26, RNF-51).
//
// Regla única y deliberada: en esta caché solo entra lo que está en ESTATICOS, y ahí no hay nada de
// ninguna cuenta. Ninguna respuesta de la red se guarda nunca. Es lo que hace que abrir la aplicación
// sin conexión y sin sesión no pueda mostrar estudios ni metadatos vistos antes (AC-41): no están
// guardados en ningún lado del navegador, ni siquiera para el dueño de la sesión.
//
// Subir la versión del nombre de la caché invalida la anterior en el próximo arranque.
const CACHE = 'archivo-medico-estatico-v1';

const PANTALLA_SIN_CONEXION = '/sin-conexion';

// Todos anónimos: si alguno dejara de serlo, el test de AC-41 falla al pedirlos sin sesión.
const ESTATICOS = [
    PANTALLA_SIN_CONEXION,
    '/manifest.webmanifest',
    '/css/site.css',
    '/js/site.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/iconos/icono-192.png',
    '/iconos/icono-512.png'
];

// Prefijos que se pueden servir desde la caché. Deliberadamente no incluye ninguna ruta de la
// aplicación: /Estudios y /Archivos entregan datos médicos y solo se resuelven contra la red.
const PREFIJOS_ESTATICOS = ['/css/', '/js/', '/lib/', '/iconos/'];

self.addEventListener('install', evento => {
    evento.waitUntil(
        caches.open(CACHE)
            .then(cache => cache.addAll(ESTATICOS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', evento => {
    evento.waitUntil(
        caches.keys()
            .then(nombres => Promise.all(
                nombres.filter(nombre => nombre !== CACHE).map(nombre => caches.delete(nombre))))
            .then(() => self.clients.claim())
    );
});

function esEstatico(url) {
    return url.pathname === '/manifest.webmanifest'
        || PREFIJOS_ESTATICOS.some(prefijo => url.pathname.startsWith(prefijo));
}

self.addEventListener('fetch', evento => {
    const solicitud = evento.request;
    const url = new URL(solicitud.url);

    // Nada que no sea una lectura de este origen pasa por acá: los envíos de formulario van directo a
    // la red para que una carga nunca se resuelva contra una copia guardada.
    if (solicitud.method !== 'GET' || url.origin !== self.location.origin) {
        return;
    }

    if (esEstatico(url)) {
        // ignoreSearch: las vistas versionan estos archivos con ?v=..., y el precargado no lleva query.
        evento.respondWith(
            caches.match(solicitud, { ignoreSearch: true })
                .then(guardada => guardada || fetch(solicitud))
        );
        return;
    }

    // Navegaciones: siempre red. Si no hay conexión, la pantalla controlada de RF-26, que es estática y
    // no contiene estudios, metadatos ni archivos (AC-40).
    if (solicitud.mode === 'navigate') {
        evento.respondWith(
            fetch(solicitud).catch(() => caches.match(PANTALLA_SIN_CONEXION, { ignoreSearch: true }))
        );
        return;
    }

    // Todo lo demás -incluida la entrega de archivos- es red y nada más: sin respaldo en caché y sin
    // escribir la respuesta.
});
