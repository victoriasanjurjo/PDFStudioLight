# Bitácora de Cambios (Changelog) - PDF Studio Light

## [3.0.0] - 2026-11-02
### Migración Completa a Rust Nativo + Tauri v2 (100% Local-First & Offline)

- **Eliminación Total de Dependencias C# y .NET**:
  - Removido WinUI 3, Windows App SDK y proyectos `.csproj` / `.slnx`.
  - Reducción drástica del tamaño en disco y memoria de ejecución (cero sobrecarga de runtime .NET o recolector de basura).
- **Backend de Procesamiento PDF en Rust Puro**:
  - Implementación con crates nativos seguros en memoria: `lopdf`, `image`, `zip`, `flate2` y `serde`.
  - Soporte de manipulación directa de flujos PDF: conteo de páginas, unión multivariable, división por rangos y páginas individuales, reordenación y rotación (`0°`, `90°`, `180°`, `270°`), compresión de flujos flate/deflate e inserción precisa de firmas y sellos gráficos con máscara alfa de transparencia (`SMask`).
  - Conversión directa de colecciones de imágenes (PNG, JPG, BMP) a páginas individuales de PDF.
- **Seguridad y Hardening en Rust**:
  - Mitigación integral contra **Zip Slip** con verificación estricta de rutas canónicas en `security::safe_extract_zip`.
  - Mitigación contra **Zip Bombs** y cuotas de seguridad estrictas (500 MB para PDFs, 100 MB para imágenes, 1 GB sin comprimir en ZIP y límite de 5.000 entradas).
  - Bloqueo de nombres reservados del sistema (`CON`, `PRN`, `AUX`, `NUL`, etc.) y saneamiento automático de nombres de archivo.
- **Frontend Moderno, Ligero y Adaptativo**:
  - Interfaz web moderna (HTML5 / CSS3 / ES6 modular) sin dependencias pesadas de Node (`node_modules`).
  - Comunicación bidireccional asíncrona mediante Tauri IPC `invoke()` con manejo limpio de errores `Result<T, String>`.
  - Lienzo interactivo de firma digital táctil y ratón con grosor ajustable y exportación directa en PNG transparente.
  - Conmutador de temas Claro / Oscuro con persistencia en `config/settings.json`.
- **Suite de Pruebas Unitarias y de Seguridad en Rust**:
  - Pruebas en `tests/security_tests.rs` verificando todas las mitigaciones de seguridad y parseo seguro de rangos.

---

## [2.1.0] - 2026-11-02
### Auditoría de Seguridad, Hardening y Mitigación de Vulnerabilidades

- Parcheo de vector de ejecución arbitraria de comandos en `SettingsViewModel`.
- Mitigación contra Zip Slip / Path Traversal en `SafeExtractZip`.
- Mitigación contra Zip Bombs y nombres reservados de Windows.
- Validación de extensiones permitidas y límites de tamaño.

---

## [2.0.0] - 2026-11-02
### Migración de Python a C# / WinUI 3 con MVVM

- Implementación inicial en .NET 8 / WinUI 3 Unpackaged con PdfSharp.
