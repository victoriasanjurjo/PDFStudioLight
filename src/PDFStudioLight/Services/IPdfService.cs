using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PDFStudioLight.Models;

namespace PDFStudioLight.Services;

/// <summary>
/// Contrato de servicios para operaciones de documentos PDF y compresión.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Obtiene la cantidad de páginas de un archivo PDF.
    /// </summary>
    int GetPageCount(string filePath);

    /// <summary>
    /// Renderiza miniaturas de todas las páginas para el visor interactivo.
    /// </summary>
    Task<List<PdfPageThumbnail>> RenderThumbnailsAsync(string filePath, uint targetWidth = 300);

    /// <summary>
    /// Renderiza una página individual a un arreglo de bytes PNG.
    /// </summary>
    Task<byte[]> RenderPageToBytesAsync(string filePath, uint pageIndex, uint targetWidth = 1200);

    /// <summary>
    /// Guarda el PDF editado conservando únicamente las páginas especificadas y aplicando rotaciones.
    /// </summary>
    Task SaveEditedPdfAsync(string inputPath, string outputPath, IList<int> pagesToKeep, IDictionary<int, int> pageRotations);

    /// <summary>
    /// Combina múltiples archivos PDF en un único documento de salida.
    /// </summary>
    Task MergePdfsAsync(IEnumerable<string> inputFiles, string outputPath);

    /// <summary>
    /// Divide un archivo PDF según rangos (ej. "1-3, 5, 8") o una página por archivo.
    /// </summary>
    Task<List<string>> SplitPdfAsync(string inputPath, string outputDirectory, string pageRanges, bool splitAllPages);

    /// <summary>
    /// Incrusta una imagen (sello, firma o gráfico) en una página del PDF.
    /// </summary>
    Task AddImageToPdfAsync(string inputPdfPath, string inputImagePath, string outputPath, int pageNumber, double x, double y, double width, double height);

    /// <summary>
    /// Convierte una lista de imágenes en un documento PDF multipágina.
    /// </summary>
    Task ConvertImagesToPdfAsync(IEnumerable<string> imagePaths, string outputPath);

    /// <summary>
    /// Comprime un documento PDF optimizando los flujos de contenido interno.
    /// </summary>
    Task CompressPdfAsync(string inputPath, string outputPath);

    /// <summary>
    /// Empaqueta una lista de archivos en un contenedor ZIP comprimido.
    /// </summary>
    Task CreateZipArchiveAsync(IEnumerable<string> filePaths, string outputZipPath);

    /// <summary>
    /// Descomprime un archivo ZIP en la carpeta de destino especificada.
    /// </summary>
    Task ExtractZipArchiveAsync(string zipPath, string destinationDirectory);
}

