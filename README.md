# PDF Studio Light

![.NET](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4)
![WinUI 3](https://img.shields.io/badge/GUI-WinUI%203-0078D7)
![MVVM](https://img.shields.io/badge/Architecture-MVVM-00A4EF)
![Engine](https://img.shields.io/badge/Engine-PdfSharp%20%2B%20Windows.Data.Pdf-0284C7)
![Privacy](https://img.shields.io/badge/Privacy-100%25_Local_%26_Offline-green)
![License](https://img.shields.io/badge/License-MIT-green)

**PDF Studio Light** es una suite de escritorio nativa para Windows (10 y 11) desarrollada en **C# (.NET 8)** con **WinUI 3 (Windows App SDK)** y el patrón arquitectónico **MVVM (Model-View-ViewModel)**. 

La herramienta está diseñada para brindar una experiencia moderna, fluida y rápida para la edición, firma, combinación, división, compresión y conversión de documentos PDF, garantizando **soberanía total y privacidad 100% local**: ninguna operación transmite datos ni telemetría fuera de tu equipo.

---

## 🚀 Características Principales

La aplicación incorpora una barra lateral basada en `NavigationView` con botón de tres rayitas (`☰`) para expandir o colapsar el menú, organizando las 9 herramientas:

1. 📝 **Editar PDF**: Visor con miniaturas generadas de forma nativa por hardware, rotación de páginas en 90°, eliminación de páginas y botón *Guardar Como...*.
2. 🔗 **Unir PDF**: Combinación de dos o más documentos con reordenamiento interactivo (*Subir* / *Bajar*).
3. ✂️ **Separar PDF**: División en páginas individuales (un archivo por página) o extracción selectiva por rangos personalizados (ej. `1-3, 5, 8-10`).
4. 🖼️ **Agregar Imagen**: Selección de cualquier imagen (PNG, JPG, BMP) e incrustación precisa con coordenadas o preajustes rápidos (*Abajo Derecha*, *Centro*, etc.).
5. ✍️ **Firmar Digitalmente**: Pizarra digital interactiva con trazo suave a mano alzada (mouse o lápiz táctil), grosor y color personalizable, con opciones para exportar como PNG transparente o estampar directamente en cualquier página del PDF.
6. 🗜️ **Comprimir y Empaquetar**: Optimización y recompresión interna de flujos de PDFs (con cálculo de porcentaje de ahorro) y empaquetador/extractor de archivos ZIP.
7. 📸 **Convertir Imágenes a PDF**: Ensamblado de múltiples fotos e imágenes en un PDF multipágina con alta fidelidad y ajuste proporcional.
8. ❓ **Centro de Ayuda y Privacidad**: Guía de uso completa y certificación formal de privacidad 100% fuera de línea (*Offline*).
9. ⚙️ **Configuración**: Conmutador dinámico de **Modo Claro** / **Modo Oscuro** / **Sistema** y selector de carpeta de guardado predeterminada.

---

## 📂 Archivos de Prueba en la Raíz

Para verificar las funciones inmediatamente tras la compilación, se conservan los archivos de muestra:
- **`ejemplo_documento.pdf`**: Documento multipágina con cláusulas de servicios y recuadro para ensayar firmas y sellos.
- **`ejemplo_imagen.png`**: Sello gráfico transparente (*"Aprobado y Firmado"*) para probar la incrustación de imágenes en la herramienta *Agregar Imagen*.

---

## 🏗️ Arquitectura y Componentes

El proyecto sigue una estructura limpia y desacoplada mediante inyección de dependencias (`Microsoft.Extensions.DependencyInjection`):

```text
PDFStudioLight/
├── assets/                         # Recursos gráficos e íconos (.ico, .png, .svg)
├── config/                         # Ajustes persistentes locales (settings.json)
├── docs/                           # Documentación técnica, justificaciones y changelog
├── outputs/                        # Carpeta sugerida para documentos de salida
├── tests/
│   └── PDFStudioLight.Tests/       # Pruebas unitarias automatizadas con xUnit
│       ├── PdfServiceTests.cs      # Pruebas de unión, división, compresión y ZIP
│       └── PDFStudioLight.Tests.csproj
├── src/
│   └── PDFStudioLight/             # Aplicación de escritorio nativa WinUI 3
│       ├── Assets/                 # Ícono oficial de la aplicación (AppIcon.ico)
│       ├── Converters/             # Conversores de enlaces de datos XAML
│       ├── Models/                 # Modelos de datos (PdfPageThumbnail, DocumentItem, AppSettings)
│       ├── Services/               # Servicios (PdfService, DialogService, SettingsService)
│       ├── ViewModels/             # ViewModels para cada herramienta (CommunityToolkit.Mvvm)
│       ├── Views/                  # Páginas XAML con diseño moderno y Fluent Design
│       ├── App.xaml / App.xaml.cs  # Inicialización, temas y contenedor de servicios (DI)
│       ├── MainWindow.xaml         # Ventana principal con TitleBar personalizada y Mica
│       ├── MainPage.xaml           # NavigationView con menú lateral colapsable
│       └── PDFStudioLight.csproj   # Configuración de compilación .NET 8 Unpackaged
├── PDFStudioLight.slnx             # Solución completa .NET
├── LICENSE.txt                     # Licencia MIT completa y vigente
└── README.md                       # Documentación general y guía de ejecución
```

---

## 🛠️ Requisitos Previos

- **Sistema Operativo**: Windows 10 (versión 1809 / Build 17763 o superior) o Windows 11.
- **SDK de .NET**: .NET SDK 8.0 o superior (compatible con .NET 10).

---

## 💻 Instrucciones de Ejecución

### 1. Ejecutar las Pruebas Unitarias
Para correr la suite de pruebas automatizadas con xUnit:

```powershell
dotnet test tests/PDFStudioLight.Tests/PDFStudioLight.Tests.csproj
```

### 2. Compilar la Solución Completa
Para compilar la solución y validar todos los proyectos:

```powershell
dotnet build PDFStudioLight.slnx
```

### 3. Ejecutar la Aplicación en Modo Desarrollo
Puedes iniciar la aplicación directamente desde la línea de comandos:

```powershell
dotnet run --project src/PDFStudioLight/PDFStudioLight.csproj
```

### 4. Compilar el Ejecutable Listo para Usar (Publicación Autónoma)
Para generar un binario ejecutable (`.exe`) independiente sin requerir instalación:

```powershell
dotnet publish src/PDFStudioLight/PDFStudioLight.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

El ejecutable resultante estará disponible en:
```text
src\PDFStudioLight\bin\Release\net8.0-windows10.0.26100.0\win-x64\publish\PDFStudioLight.exe
```

---

## 📄 Licencia

Este proyecto está distribuido bajo la licencia **MIT**. Consulta el archivo [LICENSE.txt](LICENSE.txt) para más detalles.
