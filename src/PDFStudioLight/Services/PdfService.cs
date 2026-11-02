using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using PDFStudioLight.Helpers;
using PDFStudioLight.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PDFStudioLight.Services;

/// <summary>
/// Implementación de operaciones sobre archivos PDF utilizando PdfSharp y Windows.Data.Pdf con protecciones de seguridad activas.
/// </summary>
public class PdfService : IPdfService
{
    public int GetPageCount(string filePath)
    {
        SecurityValidator.ValidatePdfFile(filePath, mustExist: true);

        using var doc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
        return doc.PageCount;
    }

    public async Task<List<PdfPageThumbnail>> RenderThumbnailsAsync(string filePath, uint targetWidth = 300)
    {
        SecurityValidator.ValidatePdfFile(filePath, mustExist: true);

        var list = new List<PdfPageThumbnail>();
        var canonicalPath = Path.GetFullPath(filePath);

        var storageFile = await StorageFile.GetFileFromPathAsync(canonicalPath);
        var pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(storageFile);

        // Limitar la cantidad máxima de páginas renderizadas en una sola tanda para prevenir agotamiento de recursos
        uint pageLimit = Math.Min(pdfDoc.PageCount, 500u);

        for (uint i = 0; i < pageLimit; i++)
        {
            using var page = pdfDoc.GetPage(i);
            var item = new PdfPageThumbnail
            {
                PageNumber = (int)(i + 1),
                OriginalWidth = page.Size.Width,
                OriginalHeight = page.Size.Height,
                RotationAngle = 0,
                IsSelected = false
            };

            using var memStream = new InMemoryRandomAccessStream();
            var renderOptions = new PdfPageRenderOptions
            {
                DestinationWidth = Math.Min(targetWidth, 1000u)
            };

            await page.RenderToStreamAsync(memStream, renderOptions);
            memStream.Seek(0);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(memStream);
            item.ThumbnailImage = bitmap;

            list.Add(item);
        }

        return list;
    }

    public async Task<byte[]> RenderPageToBytesAsync(string filePath, uint pageIndex, uint targetWidth = 1200)
    {
        SecurityValidator.ValidatePdfFile(filePath, mustExist: true);

        var storageFile = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(filePath));
        var pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(storageFile);

        if (pageIndex >= pdfDoc.PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "El índice de página está fuera de rango.");

        using var page = pdfDoc.GetPage(pageIndex);
        using var memStream = new InMemoryRandomAccessStream();
        var renderOptions = new PdfPageRenderOptions
        {
            DestinationWidth = Math.Min(targetWidth, 3000u)
        };

        await page.RenderToStreamAsync(memStream, renderOptions);
        memStream.Seek(0);

        var bytes = new byte[memStream.Size];
        using var dataReader = new DataReader(memStream.GetInputStreamAt(0));
        await dataReader.LoadAsync((uint)memStream.Size);
        dataReader.ReadBytes(bytes);

        return bytes;
    }

    public Task SaveEditedPdfAsync(string inputPath, string outputPath, IList<int> pagesToKeep, IDictionary<int, int> pageRotations)
    {
        SecurityValidator.ValidatePdfFile(inputPath, mustExist: true);
        SecurityValidator.ValidatePdfFile(outputPath, mustExist: false);

        return Task.Run(() =>
        {
            using var inputDoc = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
            using var outputDoc = new PdfSharp.Pdf.PdfDocument();

            foreach (var pageNum in pagesToKeep)
            {
                int pageIdx = pageNum - 1;
                if (pageIdx >= 0 && pageIdx < inputDoc.PageCount)
                {
                    var page = outputDoc.AddPage(inputDoc.Pages[pageIdx]);
                    if (pageRotations != null && pageRotations.TryGetValue(pageNum, out int angle))
                    {
                        int currentRot = page.Rotate;
                        page.Rotate = (currentRot + angle) % 360;
                    }
                }
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            outputDoc.Save(outputPath);
        });
    }

    public Task MergePdfsAsync(IEnumerable<string> inputFiles, string outputPath)
    {
        SecurityValidator.ValidatePdfFile(outputPath, mustExist: false);

        return Task.Run(() =>
        {
            using var outputDoc = new PdfSharp.Pdf.PdfDocument();

            foreach (var file in inputFiles)
            {
                SecurityValidator.ValidatePdfFile(file, mustExist: true);

                using var inputDoc = PdfReader.Open(file, PdfDocumentOpenMode.Import);
                for (int i = 0; i < inputDoc.PageCount; i++)
                {
                    outputDoc.AddPage(inputDoc.Pages[i]);
                }
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            outputDoc.Save(outputPath);
        });
    }

    public Task<List<string>> SplitPdfAsync(string inputPath, string outputDirectory, string pageRanges, bool splitAllPages)
    {
        SecurityValidator.ValidatePdfFile(inputPath, mustExist: true);
        SecurityValidator.ValidatePathBasics(outputDirectory);

        return Task.Run(() =>
        {
            var canonicalOutDir = Path.GetFullPath(outputDirectory);
            if (!Directory.Exists(canonicalOutDir))
            {
                Directory.CreateDirectory(canonicalOutDir);
            }

            var rawBaseName = Path.GetFileNameWithoutExtension(inputPath);
            var safeBaseName = SecurityValidator.SanitizeFileName(rawBaseName);
            var generatedFiles = new List<string>();

            using var inputDoc = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
            int totalPages = inputDoc.PageCount;

            if (splitAllPages)
            {
                for (int i = 0; i < totalPages; i++)
                {
                    using var singleDoc = new PdfSharp.Pdf.PdfDocument();
                    singleDoc.AddPage(inputDoc.Pages[i]);
                    var outPath = Path.Combine(canonicalOutDir, $"{safeBaseName}_pag_{(i + 1):D3}.pdf");
                    singleDoc.Save(outPath);
                    generatedFiles.Add(outPath);
                }
                return generatedFiles;
            }

            var ranges = ParsePageRanges(pageRanges, totalPages);
            int partIndex = 1;

            foreach (var group in ranges)
            {
                if (group.Count == 0) continue;

                using var subDoc = new PdfSharp.Pdf.PdfDocument();
                foreach (var pageNum in group)
                {
                    subDoc.AddPage(inputDoc.Pages[pageNum - 1]);
                }

                string rangeLabel = group.Count == 1 ? $"pag_{group[0]}" : $"pags_{group.First()}-{group.Last()}";
                var outPath = Path.Combine(canonicalOutDir, $"{safeBaseName}_parte_{partIndex}_{rangeLabel}.pdf");
                subDoc.Save(outPath);
                generatedFiles.Add(outPath);
                partIndex++;
            }

            return generatedFiles;
        });
    }

    public Task AddImageToPdfAsync(string inputPdfPath, string inputImagePath, string outputPath, int pageNumber, double x, double y, double width, double height)
    {
        SecurityValidator.ValidatePdfFile(inputPdfPath, mustExist: true);
        SecurityValidator.ValidateImageFile(inputImagePath, mustExist: true);
        SecurityValidator.ValidatePdfFile(outputPath, mustExist: false);

        // Sanity checks en coordenadas y dimensiones
        if (width <= 0 || height <= 0 || width > 10000 || height > 10000)
            throw new ArgumentOutOfRangeException(nameof(width), "Dimensiones de la imagen fuera del rango seguro.");

        return Task.Run(() =>
        {
            using var doc = PdfReader.Open(inputPdfPath, PdfDocumentOpenMode.Modify);
            int pageIndex = pageNumber - 1;
            if (pageIndex < 0 || pageIndex >= doc.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Número de página no válido.");

            var page = doc.Pages[pageIndex];
            using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
            {
                using var image = XImage.FromFile(inputImagePath);
                gfx.DrawImage(image, x, y, width, height);
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            doc.Save(outputPath);
        });
    }

    public Task ConvertImagesToPdfAsync(IEnumerable<string> imagePaths, string outputPath)
    {
        SecurityValidator.ValidatePdfFile(outputPath, mustExist: false);

        return Task.Run(() =>
        {
            using var doc = new PdfSharp.Pdf.PdfDocument();

            foreach (var imgPath in imagePaths)
            {
                SecurityValidator.ValidateImageFile(imgPath, mustExist: true);

                using var image = XImage.FromFile(imgPath);
                var page = doc.AddPage();
                page.Width = XUnit.FromPoint(image.PointWidth);
                page.Height = XUnit.FromPoint(image.PointHeight);

                using var gfx = XGraphics.FromPdfPage(page);
                gfx.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            doc.Save(outputPath);
        });
    }

    public Task CompressPdfAsync(string inputPath, string outputPath)
    {
        SecurityValidator.ValidatePdfFile(inputPath, mustExist: true);
        SecurityValidator.ValidatePdfFile(outputPath, mustExist: false);

        return Task.Run(() =>
        {
            using var doc = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
            doc.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
            doc.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Automatic;
            doc.Options.CompressContentStreams = true;

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            doc.Save(outputPath);
        });
    }

    public Task CreateZipArchiveAsync(IEnumerable<string> filePaths, string outputZipPath)
    {
        SecurityValidator.ValidatePathBasics(outputZipPath);

        return Task.Run(() =>
        {
            var dir = Path.GetDirectoryName(outputZipPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(outputZipPath))
            {
                File.Delete(outputZipPath);
            }

            using var archive = ZipFile.Open(outputZipPath, ZipArchiveMode.Create);
            foreach (var file in filePaths)
            {
                SecurityValidator.ValidatePathBasics(file);
                if (File.Exists(file))
                {
                    var safeName = SecurityValidator.SanitizeFileName(Path.GetFileName(file));
                    archive.CreateEntryFromFile(file, safeName, CompressionLevel.Optimal);
                }
            }
        });
    }

    public Task ExtractZipArchiveAsync(string zipPath, string destinationDirectory)
    {
        return Task.Run(() =>
        {
            SecurityValidator.SafeExtractZip(zipPath, destinationDirectory);
        });
    }

    /// <summary>
    /// Parsea una cadena de rangos como "1-3, 5, 8-10" con protección contra Denial of Service (DoS) por memoria.
    /// </summary>
    public static List<List<int>> ParsePageRanges(string expression, int maxPage)
    {
        var result = new List<List<int>>();
        if (string.IsNullOrWhiteSpace(expression)) return result;

        var parts = expression.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawPart in parts)
        {
            if (result.Count >= SecurityValidator.MaxPageGroups)
                break;

            var part = rawPart.Trim();
            if (part.Contains('-'))
            {
                var subParts = part.Split('-');
                if (subParts.Length == 2 &&
                    int.TryParse(subParts[0].Trim(), out int start) &&
                    int.TryParse(subParts[1].Trim(), out int end))
                {
                    start = Math.Max(1, Math.Min(start, maxPage));
                    end = Math.Max(1, Math.Min(end, maxPage));
                    if (start <= end)
                    {
                        var group = Enumerable.Range(start, end - start + 1).ToList();
                        if (group.Count > 0) result.Add(group);
                    }
                }
            }
            else if (int.TryParse(part, out int singlePage))
            {
                if (singlePage >= 1 && singlePage <= maxPage)
                {
                    result.Add(new List<int> { singlePage });
                }
            }
        }

        return result;
    }
}
