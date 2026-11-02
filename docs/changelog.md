# Bitácora de Cambios (Changelog) - PDF Studio Light

## [2.1.0] - 2026-11-02
### Auditoría de Seguridad, Hardening y Mitigación de Vulnerabilidades

- **Auditoría Ofensiva / Defensiva de Código**:
  - Identificación y parcheo de vector de ejecución arbitraria de comandos en `SettingsViewModel` (`UseShellExecute = true` reemplazado por invocación estricta y segura de `explorer.exe`).
  - Mitigación integral contra ataques **Zip Slip / Path Traversal** durante la extracción de archivos comprimidos mediante validación canónica de rutas (`SecurityValidator.SafeExtractZip`).
  - Mitigación contra ataques de denegación de servicio por archivos descomprimidos (**Zip Bombs**) con umbrales acumulativos de descompresión (tope de 1 GB y 5.000 entradas).
  - Mitigación contra colisiones y vulnerabilidades de nombres de dispositivos reservados de Windows (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`).
  - Saneamiento y filtrado de caracteres maliciosos en nombres de archivos generados y divididos.
  - Validación de extensiones permitidas y límites de tamaño máximo para PDFs (500 MB) e imágenes (100 MB).
- **Suite de Pruebas de Seguridad Automatizadas**:
  - Incorporación de `SecurityTests.cs` en `tests/PDFStudioLight.Tests` (13 pruebas unitarias y de hardening superadas al 100%).

## [2.0.0] - 2026-11-02
### Migración Completa de Python a C# / WinUI 3 con MVVM

- **Reescritura de Arquitectura Completa**:
  - Eliminación total del stack en Python (PySide6 / PyMuPDF / PyInstaller).
  - Creación de la solución moderna .NET 8 con **WinUI 3 (Windows App SDK)** en modo independiente desempaquetado (*Unpackaged*).
  - Adopción integral del patrón **MVVM** mediante **CommunityToolkit.Mvvm** e inyección de dependencias con `Microsoft.Extensions.DependencyInjection`.
- **Nuevo Motor de Procesamiento**:
  - Implementación de `IPdfService` y `PdfService` con `PdfSharp` v6.1.1 (Licencia MIT) para operaciones de unión, división por rangos, rotación, eliminación e inserción de imágenes.
  - Integración con la API nativa de Windows `Windows.Data.Pdf` para renderizado acelerado por hardware de miniaturas y previsualización.
- **Suite de 9 Herramientas en Interfaz Fluent Design**:
  - Menú lateral moderno con `NavigationView` y botón hamburguesa (`☰`) colapsable.
  - Fondo translúcido con efecto *Mica* y barra de título personalizada con el ícono de la aplicación.
  - Pizarra de firma digital a mano alzada con exportación transparente y estampado directo en PDFs.
  - Compresión y empaquetador ZIP integrado con `System.IO.Compression`.
  - Conmutador en tiempo real de temas Claro y Oscuro.
- **Suite de Pruebas Automatizadas**:
  - Proyecto `tests/PDFStudioLight.Tests` con `xUnit`, cubriendo validaciones de conteo de páginas, combinación, división por rangos, descompresión y compresión.
