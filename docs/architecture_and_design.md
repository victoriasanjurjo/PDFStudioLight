# Arquitectura y Diseño Técnico - PDF Studio Light (C# WinUI 3 & MVVM)

## 1. Visión General
**PDF Studio Light** es una suite de escritorio nativa para Windows 10 y Windows 11 desarrollada en **C# (.NET 8 LTS)** utilizando **WinUI 3 (Windows App SDK)** bajo el patrón de arquitectura **MVVM (Model-View-ViewModel)** con **CommunityToolkit.Mvvm**.

La premisa fundamental de diseño y arquitectura es la **Soberanía y Privacidad 100% Local**: todas las transformaciones, combinaciones, divisiones, renderizados y estampados de firmas se ejecutan estrictamente en el hardware del equipo del usuario, sin llamadas de red externas ni telemetría.

---

## 2. Decisiones Técnicas y Justificación

### 2.1 Interfaz de Usuario: WinUI 3 (Windows App SDK)
- **Motivación**: WinUI 3 es el framework oficial y moderno de Microsoft para interfaces de escritorio en Windows. Ofrece aceleración por hardware, soporte nativo para efectos Fluent Design (Mica backdrop, acrílico, animaciones fluidas), soporte para temas claro/oscuro del sistema y controles de navegación modernos (`NavigationView`, `CommandBar`, `NumberBox`).
- **Empaquetado Desempaquetado (`<WindowsPackageType>None</WindowsPackageType>`)**:
  - Permite compilar y distribuir la aplicación como un ejecutable directo (`PDFStudioLight.exe`) sin forzar la instalación mediante paquetes MSIX restringidos ni requerir certificados de firma de prueba.

### 2.2 Patrón Arquitectónico: MVVM con CommunityToolkit.Mvvm
- **Motivación**:
  - Separación total entre la vista (XAML) y la lógica de negocio y presentación.
  - Uso de generadores de código fuente (`[ObservableProperty]`, `[RelayCommand]`) que reducen el código repetitivo (*boilerplate*) y maximizan el rendimiento y la testeabilidad.
  - Inyección de dependencias mediante `Microsoft.Extensions.DependencyInjection`, desacoplando servicios como `IPdfService`, `IDialogService` e `ISettingsService`.

### 2.3 Motores de Procesamiento de Documentos
1. **PdfSharp (v6.1.1 MIT)**:
   - Utilizado para todas las manipulaciones estructurales de PDFs: unir archivos, segmentar por rangos, rotar páginas, eliminar páginas, incrustar imágenes y sellos con coordenadas milimétricas, y generar PDFs multipágina desde colecciones de imágenes.
   - Licencia MIT abierta y permisiva, alineada con la licencia del proyecto.
2. **Windows.Data.Pdf (API nativa de Windows)**:
   - Empleada para el renderizado ultra-rápido y con aceleración por hardware de miniaturas y previsualizaciones de páginas completas.
   - Completamente integrada en el sistema operativo Windows, garantizando cero dependencias pesadas de terceros para el visor interactivo.
3. **System.IO.Compression**:
   - Utilizada para la compresión ZIP y descompresión rápida en flujos locales.

---

## 3. Estructura de Componentes

```text
PDFStudioLight/
├── assets/                          # Recursos visuales (íconos .ico, .png, .svg)
├── config/                          # Configuración persistente (settings.json)
├── docs/                            # Documentación técnica y decisiones de diseño
├── outputs/                         # Carpeta sugerida para exportación de archivos
├── tests/
│   └── PDFStudioLight.Tests/        # Pruebas unitarias automatizadas con xUnit
│       ├── PdfServiceTests.cs       # Validación de unión, división, compresión y ZIP
│       └── PDFStudioLight.Tests.csproj
├── src/
│   └── PDFStudioLight/              # Proyecto principal WinUI 3 Desktop
│       ├── Assets/                  # Íconos y logotipos de la aplicación
│       ├── Converters/              # Conversores XAML (BoolToVisibility, etc.)
│       ├── Models/                  # Modelos de datos (PdfPageThumbnail, DocumentItem, AppSettings)
│       ├── Services/                # Capa de servicios (PdfService, DialogService, SettingsService)
│       ├── ViewModels/              # ViewModels por cada pantalla y herramienta
│       ├── Views/                   # Vistas XAML de cada módulo
│       ├── App.xaml / App.xaml.cs   # Inicialización y contenedor de dependencias (DI)
│       ├── MainWindow.xaml          # Ventana principal con barra de título personalizada y Mica
│       ├── MainPage.xaml            # NavigationView con barra lateral colapsable (☰)
│       └── PDFStudioLight.csproj    # Definición del proyecto .NET 8 Unpackaged
├── PDFStudioLight.slnx              # Solución completa .NET
├── LICENSE.txt                      # Licencia MIT vigente
└── README.md                        # Documentación general y guía de ejecución
```

---

## 4. Módulos y Funcionalidades

1. **Editar PDF (`EditPdfPage` / `EditPdfViewModel`)**:
   - Carga de documentos, generación reactiva de miniaturas, rotación 90° de páginas, eliminación de páginas y exportación con "Guardar como...".
2. **Unir PDF (`MergePdfPage` / `MergePdfViewModel`)**:
   - Inserción de múltiples archivos, reordenamiento interactivo (*Subir* / *Bajar*) y combinación en un único documento.
3. **Separar PDF (`SplitPdfPage` / `SplitPdfViewModel`)**:
   - División de todas las páginas en archivos individuales o extracción selectiva por rangos (ej. `1-3, 5, 8-10`).
4. **Agregar Imagen / Sello (`AddImagePage` / `AddImageViewModel`)**:
   - Incrustación de gráficos transparentes o imágenes con presets de ubicación rápida (*Abajo Derecha*, *Centro*, etc.) y coordenadas precisas en puntos tipográficos.
5. **Firmar (`SignPdfPage` / `SignPdfViewModel`)**:
   - Pizarra de dibujo libre interactiva con puntero o lápiz, ajuste de grosor de trazo, selector de color (Negro, Azul, Rojo), exportación a PNG transparente y estampado directo sobre el documento PDF.
6. **Comprimir / ZIP (`CompressPage` / `CompressPdfViewModel`)**:
   - Optimización de flujos internos de PDFs para reducción de peso con cálculo del porcentaje de ahorro, y empaquetador/extractor de archivos ZIP.
7. **Imágenes a PDF (`ImagesToPdfPage` / `ImagesToPdfViewModel`)**:
   - Conversión de múltiples imágenes (PNG, JPG, BMP) a un documento PDF multipágina con ajuste proporcional exacto.
8. **Centro de Ayuda (`HelpPage` / `HelpViewModel`)**:
   - Guía de cada herramienta y certificación formal de privacidad 100% local.
9. **Configuración (`SettingsPage` / `SettingsViewModel`)**:
   - Selector dinámico de tema visual (Claro, Oscuro o Sistema) y asignación de carpeta de salida preferida.

---

## 5. Muro de Seguridad y Hardening (`SecurityValidator`)

Para garantizar la protección contra vectores de ataque comunes en aplicaciones de escritorio que procesan archivos, se implementó el módulo [`SecurityValidator.cs`](file:///d:/Proyectos/Repos/PDFStudioLight/src/PDFStudioLight/Helpers/SecurityValidator.cs):

1. **Prevención de Ejecución Arbitraria (Process Injection / Command Execution)**:
   - Eliminación del uso de `UseShellExecute = true` con rutas dinámicas. La apertura de carpetas se realiza invocando estrictamente `explorer.exe` con rutas canónicas entrecomilladas.
2. **Mitigación de Zip Slip (Path Traversal)**:
   - Inspección rigurosa de cada entrada de archivo ZIP durante la extracción. Se verifica que la ruta de destino resuelta esté estrictamente contenida dentro del directorio objetivo canónico, arrojando `SecurityException` ante cualquier intento de escape (`../`).
3. **Protección contra Zip Bombs y Agotamiento de Recursos (DoS)**:
   - Límite de 5.000 entradas máximas por archivo comprimido.
   - Límite de tamaño total descomprimido acumulativo de 1 GB.
   - Límite de memoria en segmentación de rangos de páginas.
4. **Validación de Archivos y Nombres Reservados**:
   - Bloqueo de nombres de dispositivos DOS/Windows reservados (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`).
   - Verificación estricta de extensiones permitidas (`.pdf` para documentos; `.png`, `.jpg`, `.jpeg`, `.bmp` para imágenes).
   - Umbral de tamaño máximo de archivo de 500 MB para PDFs y 100 MB para imágenes.
   - Saneamiento automático de caracteres ilegales en nombres de archivos generados.
