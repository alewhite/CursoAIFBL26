# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [Unreleased]

### Added
- FEAT-001a: arranque de la solución (.NET 8, EF Core + SQLite en WAL fuera del árbol de la
  aplicación) y alta administrativa de hasta 5 cuentas desde configuración externa, con
  contraseñas hasheadas mediante PBKDF2-HMAC-SHA256 (100.000 iteraciones). Sin superficie HTTP
  todavía: login y rutas privadas llegan con FEAT-001b.
