# PDF Studio Light (Tauri v2 + Rust)

[![Rust](https://img.shields.io/badge/Rust-1.75%2B-orange.svg)](https://www.rust-lang.org/)
[![Tauri](https://img.shields.io/badge/Tauri-v2-blue.svg)](https://v2.tauri.app/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)
[![Offline First](https://img.shields.io/badge/Privacy-100%25%20Offline-success.svg)](#seguridad-y-privacidad)

**PDF Studio Light** es una suite de herramientas de escritorio nativa, moderna y de alto rendimiento diseñada para la visualización, unión, división, edición, compresión, firmado y conversión de documentos PDF e imágenes.

Construida desde cero con **Rust** y el runtime de **Tauri v2**, la aplicación opera bajo una estricta filosofía **100% Local-First**: no envía telemetría, no realiza peticiones a la nube y mantiene todos los datos estrictamente dentro de tu equipo local con protecciones integradas contra ataques de desbordamiento de rutas y agotamiento de recursos.

---

## 🚀 Características Principales

1. **📑 Unir PDFs:** Combina múltiples archivos PDF en un solo documento conservando marcadores y estructura interna.
2. **✂️ Dividir PDF:** Extrae páginas individuales en lote o divide el documento en grupos mediante expresiones de rangos flexibles (ej. `1-3, 5, 8-10`).
3. **📝 Organizar y Rotar Páginas:** Reordena, elimina y rota páginas individuales (`0°`, `90°`, `180°`, `270°`) con previsualización reactiva.
4. **🗜️ Comprimir PDF & Empaquetador ZIP:**
   - Optimiza flujos binarios internos de objetos PDF (`flate`) reduciendo el peso en disco.
   - Crea archivos comprimidos `.zip` con compresión óptima y extracción segura.
5. **🖼️ Incrustar Sellos o Imágenes:** Inserta logotipos, sellos o membretes en coordenadas exactas (`X`, `Y`) y dimensiones personalizadas.
6. **✍️ Firma Digital Gráfica:** Dibuja firmas a mano alzada en un lienzo transparente con grosor dinámico o carga firmas existentes en formato PNG/JPG para estamparlas con soporte de máscara de transparencia (`SMask`).
7. **🖼️➡️📄 Convertir Imágenes a PDF:** Convierte colecciones de imágenes (`PNG`, `JPG`, `JPEG`, `BMP`) en un documento PDF multipágina.
8. **⚙️ Configuración y Temas:** Modo Oscuro y Claro con detección automática de preferencias del sistema operativo y rutas personalizables.

---

## 🛡️ Seguridad y Privacidad (Local-First)

- **Prevención de Zip Slip:** Validación estricta de rutas canónicas en `security::safe_extract_zip` impidiendo que entradas maliciosas (`../`) sobrepasen el directorio de extracción.
- **Mitigación de Zip Bombs:** Límite máximo de descompresión de 1 GB y tope de 5.000 entradas para prevenir denegación de servicio (DoS).
- **Filtro de Nombres Reservados en Windows:** Bloqueo automático de nombres reservados del sistema (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`).
- **Cuotas de Tamaño:** Protección contra archivos inflados (límite de 500 MB para PDFs y 100 MB para imágenes).

---

## 🏛️ Arquitectura del Sistema

La arquitectura desacopla el núcleo de cálculo intensivo del renderizado de la interfaz mediante el bus IPC seguro de Tauri v2:

```
PDFStudioLight/
├── Cargo.toml                    # Workspace de Rust
├── src-tauri/                    # Backend Nativo (Rust + Tauri v2)
│   ├── Cargo.toml                # Dependencias (tauri, lopdf, image, zip, serde)
│   ├── build.rs                  # Script de compilación de Tauri
│   ├── tauri.conf.json           # Configuración de ventana, empaquetado e íconos
│   ├── capabilities/             # Permisos de Tauri v2 (default.json)
│   ├── icons/                    # Íconos nativos (.ico, .png)
│   └── src/
│       ├── main.rs               # Entrypoint del ejecutable de Windows
│       ├── lib.rs                # Registro de plugins y controladores IPC
│       ├── models/               # Structs serializables con Serde
│       ├── services/             # Motores de PDF, ZIP, seguridad y ajustes
│       └── commands/             # Handlers expuestos al frontend
├── ui/                           # Frontend Nativo (HTML5 / CSS3 / ES6)
│   ├── index.html                # Estructura principal y navegación
│   ├── css/
│   │   └── style.css             # Estilos responsivos y temas Claro/Oscuro
│   ├── js/
│   │   ├── app.js                # Controlador de interfaz y llamadas invoke()
│   │   └── signature.js          # Motor del lienzo de dibujo de firmas
│   └── assets/                   # Recursos visuales e íconos
├── assets/                       # Recursos originales del proyecto
├── config/                       # settings.json local
├── docs/                         # Documentación técnica y changelog
└── tests/                        # Pruebas de integración y seguridad
```

---

## 🛠️ Requisitos Previos

- **Rust:** Toolchain estable (`1.75+`) instalada vía `rustup`:
  ```powershell
  rustup default stable
  ```
- **CLI de Tauri v2 (opcional pero recomendada para desarrollo rápido):**
  ```powershell
  cargo install tauri-cli --version "^2.0.0"
  ```
- **Microsoft C++ Build Tools** o Visual Studio con herramientas de C++ (requerido por el compilador de Rust en Windows).

---

## 💻 Instrucciones de Ejecución y Compilación

### 1. Ejecutar las Pruebas Unitarias y de Seguridad
```powershell
cargo test
```

### 2. Ejecutar en Modo Desarrollo con Recarga Rápida
```powershell
# Usando la CLI de Tauri:
cargo tauri dev

# O directamente mediante Cargo desde la carpeta src-tauri:
cd src-tauri
cargo run
```

### 3. Compilar el Binario Independiente Standalone (.exe)
Para generar el ejecutable optimizado para distribución y producción:
```powershell
# Compilación estándar con Cargo:
cd src-tauri
cargo build --release

# O empaquetado completo con instalador vía Tauri CLI:
cargo tauri build
```

El ejecutable standalone se generará en:
```text
src-tauri/target/release/pdf_studio_light.exe
```

---

## 📄 Licencia

Este proyecto está distribuido bajo la licencia **MIT**. Consulta el archivo [LICENSE.txt](LICENSE.txt) para más detalles.
