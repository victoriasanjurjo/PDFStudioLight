# Arquitectura y Diseño Técnico - PDF Studio Light (Tauri v2 + Rust)

## 1. Visión General del Sistema

**PDF Studio Light** es una suite de escritorio nativa para la gestión y manipulación de documentos PDF, imágenes y paquetes ZIP. Diseñada bajo una filosofía estricta **100% Local-First**, la aplicación opera de forma totalmente desconectada de internet, sin telemetría, sin rastreadores y sin dependencias de servicios en la nube.

```
┌─────────────────────────────────────────────────────────────┐
│                    UI (HTML5 + CSS3 + JS)                   │
│   [Unir]  [Dividir]  [Editar]  [Comprimir]  [Firma]  [Img2Pdf]  │
└──────────────────────────────┬──────────────────────────────┘
                               │ IPC invoke()
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                   Tauri v2 Core Runtime                      │
│       (Plugins: dialog, fs, opener, capabilities JSON)      │
└──────────────────────────────┬──────────────────────────────┘
                               │ Rust FFI / Safe Calls
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                  Módulos de Servicio en Rust                 │
│  ┌──────────────┐   ┌──────────────┐   ┌─────────────────┐  │
│  │   pdf_ops    │   │   zip_ops    │   │    security     │  │
│  │   (lopdf)    │   │    (zip)     │   │ (ZipSlip/Limits)│  │
│  └──────────────┘   └──────────────┘   └─────────────────┘  │
│  ┌──────────────┐   ┌──────────────┐                        │
│  │   settings   │   │  image_ops   │                        │
│  │ (serde_json) │   │   (image)    │                        │
│  └──────────────┘   └──────────────┘                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Pila Tecnológica

- **Backend / Core:** Rust 2021 + Runtime Tauri v2.
- **Manipulación de PDF:** Crate `lopdf` (versión 0.34) para manipulación de árboles de objetos, diccionarios de páginas, flujos de contenido y compresión flate.
- **Procesamiento Gráfico:** Crate `image` (PNG, JPEG, BMP) y soporte de canal Alfa con máscaras `SMask` en PDF.
- **Empaquetado Seguro:** Crate `zip` con mitigación activa contra Zip Slip y Zip Bombs.
- **Frontend:** Vanilla HTML5 / CSS3 / ES6 modular servido localmente sin dependencias de Node.js.
- **Persistencia:** Almacenamiento local directo en `config/settings.json` mediante `serde_json`.

---

## 3. Matriz de Comandos Tauri (`#[tauri::command]`)

| Comando | Entrada IPC | Salida Rust (`Result<T, String>`) | Descripción |
| :--- | :--- | :--- | :--- |
| `get_page_count` | `filePath: String` | `u32` | Conteo de páginas de un documento PDF. |
| `get_file_info` | `filePath: String` | `DocumentItemInfo` | Metadatos (tamaño formateado, páginas, nombre). |
| `merge_pdfs` | `files: Vec<String>, outputPath: String` | `MergeResult` | Combina múltiples PDFs re-enumerando IDs de objetos. |
| `split_pdf` | `inputPath, outputDir, ranges, splitAll` | `Vec<String>` | Extrae páginas o separa por rangos (ej: `1-2, 5`). |
| `save_edited_pdf` | `inputPath, outputPath, pagesToKeep, rotations` | `()` | Reorganiza y rota páginas individuales (`90°`, `180°`, `270°`). |
| `compress_pdf` | `inputPath, outputPath` | `CompressionResult` | Comprime flujos internos y reporta ahorro porcentual. |
| `convert_images_to_pdf` | `images: Vec<String>, outputPath: String` | `()` | Empaqueta imágenes en páginas consecutivas de PDF. |
| `add_image_to_pdf` | `inputPdf, inputImage, outputPath, page, x, y, w, h` | `()` | Inserta una imagen o sello en una página y posición dada. |
| `stamp_signature` | `inputPdf, signatureData, outputPath, page, x, y, w, h` | `()` | Estampa firma a mano alzada (Base64) con canal Alfa (`SMask`). |
| `create_zip_archive` | `files: Vec<String>, outputZipPath: String` | `()` | Empaqueta archivos en un archivo comprimido .zip. |
| `extract_zip_archive` | `zipPath: String, destinationDir: String` | `Vec<String>` | Extrae archivos con validación estricta de rutas. |
| `load_settings` | *Ninguna* | `AppSettings` | Carga preferencias locales (tema, ruta de salida). |
| `save_settings` | `settings: AppSettings` | `()` | Persiste configuración en disco. |

---

## 4. Arquitectura de Seguridad y Hardening

1. **Prevención de Zip Slip:**
   Toda extracción en `security::safe_extract_zip` comprueba que la ruta canónica de cada entrada resida estrictamente dentro de la ruta canónica del directorio de destino (`outpath.starts_with(&canonical_dest)`). Si se detecta un intento de desbordamiento de directorio relativo (`../`), la operación se cancela de inmediato emitiendo un error.
2. **Mitigación contra Zip Bombs:**
   Se mantiene un contador acumulativo de bytes descomprimidos con un límite de 1 GB (`MAX_ZIP_UNCOMPRESSED_BYTES`) y un límite máximo de 5.000 entradas (`MAX_ZIP_ENTRY_COUNT`).
3. **Validación de Límites y Nombres Reservados de Windows:**
   Se bloquean de forma terminante nombres de dispositivos de Windows (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`) y se desinfectan nombres de archivo eliminando caracteres conflictivos (`/\:*?"<>|`).
4. **Cuotas Máximas de Memoria:**
   Archivos PDF: Máximo 500 MB.
   Archivos de Imagen: Máximo 100 MB.
