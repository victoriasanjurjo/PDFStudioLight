using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PDFStudioLight.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Xunit;

namespace PDFStudioLight.Tests;

public class PdfServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly PdfService _service;

    public PdfServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "PDFStudioLightTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _service = new PdfService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch { }
    }

    private string CreateSamplePdf(string fileName, int pageCount)
    {
        string filePath = Path.Combine(_testDir, fileName);
        using var doc = new PdfDocument();
        for (int i = 0; i < pageCount; i++)
        {
            var page = doc.AddPage();
            using var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 16);
            gfx.DrawString($"Página {i + 1}", font, XBrushes.Black, new XPoint(50, 50));
        }
        doc.Save(filePath);
        return filePath;
    }

    [Fact]
    public void GetPageCount_ReturnsCorrectCount()
    {
        var pdf = CreateSamplePdf("test_count.pdf", 4);
        int count = _service.GetPageCount(pdf);
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task MergePdfs_CombinesMultipleDocuments()
    {
        var doc1 = CreateSamplePdf("doc1.pdf", 2);
        var doc2 = CreateSamplePdf("doc2.pdf", 3);
        var outputPath = Path.Combine(_testDir, "merged.pdf");

        await _service.MergePdfsAsync(new[] { doc1, doc2 }, outputPath);

        Assert.True(File.Exists(outputPath));
        int count = _service.GetPageCount(outputPath);
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task SplitPdf_SplitAllPages_CreatesIndividualFiles()
    {
        var doc = CreateSamplePdf("doc_split.pdf", 3);
        var outDir = Path.Combine(_testDir, "split_all");

        var results = await _service.SplitPdfAsync(doc, outDir, string.Empty, splitAllPages: true);

        Assert.Equal(3, results.Count);
        foreach (var file in results)
        {
            Assert.True(File.Exists(file));
            Assert.Equal(1, _service.GetPageCount(file));
        }
    }

    [Fact]
    public async Task SplitPdf_CustomRanges_ExtractsExpectedGroups()
    {
        var doc = CreateSamplePdf("doc_ranges.pdf", 5);
        var outDir = Path.Combine(_testDir, "split_ranges");

        var results = await _service.SplitPdfAsync(doc, outDir, "1-2, 4-5", splitAllPages: false);

        Assert.Equal(2, results.Count);
        Assert.Equal(2, _service.GetPageCount(results[0]));
        Assert.Equal(2, _service.GetPageCount(results[1]));
    }

    [Fact]
    public void ParsePageRanges_ParsesRangesAccurately()
    {
        var ranges = PdfService.ParsePageRanges("1-3, 5, 8-10", 10);

        Assert.Equal(3, ranges.Count);
        Assert.Equal(new List<int> { 1, 2, 3 }, ranges[0]);
        Assert.Equal(new List<int> { 5 }, ranges[1]);
        Assert.Equal(new List<int> { 8, 9, 10 }, ranges[2]);
    }

    [Fact]
    public async Task ZipAndUnzip_PacksAndExtractsFiles()
    {
        var doc1 = CreateSamplePdf("file1.pdf", 1);
        var doc2 = CreateSamplePdf("file2.pdf", 2);
        var zipPath = Path.Combine(_testDir, "archive.zip");
        var extractDir = Path.Combine(_testDir, "extracted");

        await _service.CreateZipArchiveAsync(new[] { doc1, doc2 }, zipPath);
        Assert.True(File.Exists(zipPath));

        await _service.ExtractZipArchiveAsync(zipPath, extractDir);
        Assert.True(File.Exists(Path.Combine(extractDir, "file1.pdf")));
        Assert.True(File.Exists(Path.Combine(extractDir, "file2.pdf")));
    }

    [Fact]
    public async Task CompressPdf_OptimizesDocumentStream()
    {
        var doc = CreateSamplePdf("to_compress.pdf", 3);
        var compressedPath = Path.Combine(_testDir, "compressed.pdf");

        await _service.CompressPdfAsync(doc, compressedPath);

        Assert.True(File.Exists(compressedPath));
        Assert.Equal(3, _service.GetPageCount(compressedPath));
    }
}

